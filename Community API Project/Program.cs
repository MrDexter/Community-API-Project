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
   var result = new List<Dictionary<string, object>>();
   using (var connection = new MySqlConnection(connectionString))
    {
        await connection.OpenAsync();

        var sql = "Select * From players";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i=0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            };
            result.Add(row);
        };
    };
    return Results.Ok(result);
});

app.MapGet("/players/{id}", async (int id) =>
{
    var result = new List<Dictionary<string, object>>();
    using (var connection = new MySqlConnection(connectionString))
    {
      await connection.OpenAsync();

      var sql = "Select * FROM players where uid = @uid"; // Setup to use UID or Steam ID
        // Add Houses, Player Stats
      using var command = new MySqlCommand(sql, connection);
      command.Parameters.AddWithValue("@uid", id);

      using var reader = await command.ExecuteReaderAsync();

      var row = new Dictionary<string, object>();

      while (await reader.ReadAsync())
        {
            for (int i=0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            };
        };
        result.Add(row);
    };
    return Results.Ok(result);
});

app.MapGet("/gangs", async () =>
{
    
});

app.MapGet("/gangs/{id}", async () =>
{
    
    // Pull players where 
});

app.MapPost("/player", [Authorize] async (HttpContext ctx, int id, string rank, string newRank) => // newRank is Enum is DB, Send as String
{
    if (!ctx.User.HasClaim("scope", "rank.write"))
    {
        return Results.Forbid();
    };
    using (var connection = new MySqlConnection(connectionString))
    {
        await connection.OpenAsync();

        string column = rank; // Change to Switch for more security (only edit wanted rows)

        var sql = $"Update players SET {column} = @newRank where uid = @uid";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@uid", id);
        command.Parameters.AddWithValue("@newRank", newRank);
        int reader = await command.ExecuteNonQueryAsync();
        if (reader > 0)
        {
            return Results.Ok("Success");
        } 
        else
        {
            return Results.Problem("Change Failed!");
        }
    };
});

// Chat GPT for JWT Keys, Learn about Secrets for authorization
app.MapPost("/auth/token", () =>
{
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, "admin"),
        new Claim("scope", "rank.write")
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
// app.MapPost("/auth/token", (HttpContext ctx, IConfiguration config) =>
// {
//     var expectedSecret = config["Auth:ClientSecret"];
//     if (!ctx.Request.Headers.TryGetValue("X-Auth-Secret", out var provided) ||
//         !string.Equals(provided, expectedSecret, StringComparison.Ordinal))
//     {
//         return Results.Unauthorized();
//     }

//     var claims = new[]
//     {
//         new Claim(ClaimTypes.Name, "admin"),
//         new Claim("scope", "rank.write")
//     };

//     var key = new SymmetricSecurityKey(
//         Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
//     var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//     var token = new JwtSecurityToken(
//         issuer: config["Jwt:Issuer"],
//         audience: config["Jwt:Audience"],
//         claims: claims,
//         expires: DateTime.UtcNow.AddHours(1),
//         signingCredentials: creds);

//     return Results.Ok(new
//     {
//         token = new JwtSecurityTokenHandler().WriteToken(token)
//     });
// });



app.Run();

