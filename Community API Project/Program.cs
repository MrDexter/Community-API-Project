using System.Runtime.InteropServices;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var token = builder.Configuration["AuthTokens:MasterToken"];

builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true, 

      ValidIssuer = builder.Configuration["Jwt:Issuer"],
      ValidAudience = builder.Configuration["Jwt:Audience"],

      IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
      )
    };
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/players", async () =>
{
   var result = new List<Player>();
   using (var connection = new MySqlConnection(connectionString))
    {
        await connection.OpenAsync();

        var sql = "Select uid, name, playerid, cash, bankacc, cartelCredits, adminLevel, copLevel, ionLevel, medicLevel, last_seen, insert_time From players";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Player(
                reader["uid"].ToString() ?? string.Empty,
                reader["name"].ToString() ?? string.Empty,
                reader["playerid"].ToString() ?? string.Empty,
                reader["cash"].ToString() ?? string.Empty,
                reader["bankacc"].ToString() ?? string.Empty,
                reader["cartelCredits"].ToString() ?? string.Empty,
                reader["adminLevel"].ToString() ?? string.Empty,
                reader["copLevel"].ToString() ?? string.Empty,
                reader["ionLevel"].ToString() ?? string.Empty,
                reader["medicLevel"].ToString() ?? string.Empty,
                reader.GetDateTime(reader.GetOrdinal("last_seen")),
                reader.GetDateTime(reader.GetOrdinal("insert_time"))
            );
            result.Add(row);
        };
    };
    return Results.Ok(result);
});

app.MapGet("/players/{id}", async (string id) => // Add Player Stats
{
    var result = new List<Dictionary<string, object>>();
    var row = new Dictionary<string, object>();
    using (var connection = new MySqlConnection(connectionString))
    {
      await connection.OpenAsync();
      var sql = "Select * FROM players WHERE uid = @uid OR playerid = @uid";
      using var command = new MySqlCommand(sql, connection);
      command.Parameters.AddWithValue("@uid", id);
      using var reader = await command.ExecuteReaderAsync();
      if (!await reader.ReadAsync())
        {
            return Results.NotFound();
        };
        for (int i=0; i < reader.FieldCount; i++)
        {
            row[reader.GetName(i)] = reader.GetValue(i);
        };
        result.Add(row);
    };
    // Housing
    using (var connnection2 = new MySqlConnection(connectionString))
    {
      await connnection2.OpenAsync();
      var sql2 = "SELECT a.id, a.location, a.securityLevel, b.VirtualContents, a.timeBought FROM housing a INNER JOIN housinginvstorage b ON (a.HousingInvStorageID=b.id) WHERE a.alive = 1 AND a.ownerPid=@pid AND a.isOrgHouse=0";
      using var command2 = new MySqlCommand(sql2, connnection2);
      command2.Parameters.AddWithValue("@pid", result[0]["playerid"]);
      using var reader2 = await command2.ExecuteReaderAsync();
      var housing = new Dictionary<string, object>();
      var count = 0;
        while (await reader2.ReadAsync())
        {
            var row2 = new Houses(
                reader2["id"].ToString() ?? string.Empty,
                reader2["location"].ToString() ?? string.Empty,
                reader2["securityLevel"].ToString() ?? string.Empty,
                reader2["virtualContents"].ToString() ?? string.Empty,
                reader2.GetDateTime(reader2.GetOrdinal("timeBought"))
            );
            count = count + 1;
            housing["House " + count ] = row2;
        };
        row["housing"] = housing;
    };
    // Vehicles
    using (var connection3 = new MySqlConnection(connectionString))
    {
        await connection3.OpenAsync();
        var sql3 = "Select id, side, classname, type, inventory, reg, capacity, security, acceleration, insert_time FROM vehicles where pid = @pid";
        using var command3 = new MySqlCommand(sql3, connection3);
        command3.Parameters.AddWithValue("@pid", result[0]["playerid"]);
        using var reader3 = await command3.ExecuteReaderAsync();
        var vehicles = new Dictionary<string, object>();
        var count = 0;
        while (await reader3.ReadAsync())
        {
            var row3 = new Vehicles(
                reader3["id"].ToString() ?? string.Empty,
                reader3["side"].ToString() ?? string.Empty,
                reader3["classname"].ToString() ?? string.Empty,
                reader3["type"].ToString() ?? string.Empty,
                reader3["inventory"].ToString() ?? string.Empty,
                reader3["reg"].ToString() ?? string.Empty,
                reader3["capacity"].ToString() ?? string.Empty,
                reader3["security"].ToString() ?? string.Empty,
                reader3["acceleration"].ToString() ?? string.Empty,
                reader3.GetDateTime(reader3.GetOrdinal("insert_time"))
            );
            count = count + 1;
            vehicles["Vehicle " + count ] = row3;
        };
        row["vehicles"] = vehicles;
    };
    return Results.Ok(result);
});

app.MapGet("/gangs", async () =>
{
var result = new List<Gangs>();
 using (var connection = new MySqlConnection(connectionString))
    {
      await connection.OpenAsync();

      var sql = "Select * FROM organisations WHERE alive = 1";

      using var command = new MySqlCommand(sql, connection);

      using var reader = await command.ExecuteReaderAsync();

      while (await reader.ReadAsync())
        {        
            var row = new Gangs(
                reader["id"].ToString() ?? string.Empty,
                reader["name"].ToString() ?? string.Empty,
                reader["members"].ToString() ?? string.Empty,
                reader["leader"].ToString() ?? string.Empty,
                reader["tag"].ToString() ?? string.Empty,
                reader["bank"].ToString() ?? string.Empty
            );
            result.Add(row);
        };
    };
    return Results.Ok(result);
});

app.MapGet("/gangs/{id}", async () =>
{
    
    // Pull players where 
});

app.MapPost("/players/{id}/updaterank", [Authorize] async (HttpContext ctx, int id, string rank, string newRank) => // newRank is Enum in DB, Send as String
{
    if (!ctx.User.HasClaim("scope", "write"))
    {
        return Results.Forbid();
    };
    var group = ctx.User.FindFirst("side")?.Value;
    string column = group switch
    {
        "police" => rank switch
        {
            "coplevel" or "tfuLevel" or "ncaLevel" or "npaslevel" or "mpuLevel" or "acadLevel" => rank,
            _ => throw new Exception("Invalid Rank Or Permissions")
        },
        "opfor" => rank switch
        {
            "ionlevel" or "deltalevel" or "UmLevel" or "iaflevel" or "irulevel" => rank,
            _ => throw new Exception("Invalid Rank Or Permissions")
        },
        "medic" => rank switch 
        {
            "mediclevel" or "hemslevel" or "hartlevel" => rank,
            _ => throw new Exception("Invalid Rank Or Permissions")
        },
        "staff" => rank switch
        {
            "adminlevel" or "donorlevel" or "donorexpiry" => rank,
            _ => throw new Exception("Invalid Rank Or Permissions")  
        },
        //Error
         _ => throw new Exception("Invalid Permissions")
    };
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    UpdateRankGet? oldData = null;
    var sqlGet = $"Select name, {rank} FROM players WHERE uid = @id";
    using (var getCommand = new MySqlCommand(sqlGet, connection))
    {
        getCommand.Parameters.AddWithValue("@rank", rank);
        getCommand.Parameters.AddWithValue("@id", id);
        using var reader = await getCommand.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return Results.NotFound();
        };
          
        oldData = new UpdateRankGet(
            reader["name"].ToString() ?? string.Empty,
            reader[rank].ToString() ?? string.Empty
        );
    };

    var sql = $"Update players SET {column} = @newRank where uid = @uid";

    using (var command = new MySqlCommand(sql, connection))
    {    
        command.Parameters.AddWithValue("@uid", id);
        command.Parameters.AddWithValue("@newRank", newRank);
        int reader = await command.ExecuteNonQueryAsync();
        var oldRank = oldData.OldValue;
        var name = oldData.Name;
        var result = new UpdateRank (
            id.ToString() ?? string.Empty,
            name.ToString() ?? string.Empty,
            rank.ToString() ?? string.Empty,
            oldRank.ToString() ?? string.Empty,
            newRank.ToString() ?? string.Empty,
            reader > 0 ? "Success" : "Failed"
        );
        return Results.Ok(result);
    };
});

// Chat GPT for JWT Keys, Learn about Secrets for authorization
app.MapPost("/auth/token", (HttpContext ctx, IConfiguration config) =>
{
    var expectedSecret = config["AuthTokens:ClientSecret"]; 
    if (!ctx.Request.Headers.TryGetValue("X-Auth-Secret", out var provided) ||
        !string.Equals(provided, expectedSecret, StringComparison.Ordinal))
    {
        return Results.Ok(provided);
    };

    var name  = ctx.Request.Query["name"].ToString();
    var group = ctx.Request.Query["group"].ToString();
    var perm = ctx.Request.Query["perms"].ToString();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, name),
        new Claim("side", group),
        new Claim("scope", perm)
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token)
    });
});


app.Run();

record Player (
    string Id,
    string Name,
    string PlayerId,
    string Cash,
    string Bankacc,
    string CartelCredits,
    string AdminLevel,
    string CopLevel,
    string IonLevel,
    string MedicLevel,
    DateTime LastSeen,
    DateTime InsertTime
);

record Vehicles (
    string Id,
    string Side,
    string Class,
    string Type,
    string Inventory,
    string Reg,
    string Capacity,
    string Security,
    string Acceleration,
    DateTime InsertTime
);

record Houses (
    string Id,
    string Location,
    string SecurityLevel,
    string VirtualContents,
    DateTime TimeBought
);

record Gangs (
    string Id,
    string Name,
    string Members,
    string Leader,
    string Tag,
    string Bank
);

record UpdateRankGet (
    string Name,
    string OldValue
);

record UpdateRank (
    string Id,
    string Name,
    string RankName,
    string OldValue,
    string NewValue,
    string Outcome
);