using MySqlConnector;
using CommunityApi.Models;

namespace CommunityApi.Services;

public interface IGangService
{
    Task<List<Gangs>>GetAllGangs();   
    Task<List<Dictionary<string,object>>>GetGang(string id);
}

public class GangService : IGangService
{
    private readonly string connectionString;
    public GangService(IConfiguration config)
    {
        connectionString = config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing Default Connection");
    }

    public async Task<List<Gangs>>GetAllGangs()
    {
        var result = new List<Gangs>();
        using (var connection = new MySqlConnection(connectionString))
        {
        await connection.OpenAsync();

        var sql = "Select id, name, members, leader, tag, bank FROM organisations WHERE alive = 1";

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
        return(result);
    }

    public async Task<List<Dictionary<string, object>>>GetGang(string id)
    {
        var result = new List<Dictionary<string, object>>();
        var row = new Dictionary<string, object>();
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        var sql = "Select * FROM organisations WHERE alive = 1 and id = @id";
        using (var command = new MySqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@id", id);
            using var reader = await command.ExecuteReaderAsync();
            if(!await reader.ReadAsync())
            {
                return null;
            };
            for (int i=0; i < reader.FieldCount; i++)
            {
              row[reader.GetName(i)] = reader.GetValue(i);  
            };
            result.Add(row);
        };
        // Housing
        var sql2 = "SELECT a.id, a.location, a.securityLevel, b.VirtualContents, a.timeBought FROM housing a INNER JOIN housinginvstorage b ON (a.HousingInvStorageID=b.id) WHERE a.alive = 1 AND a.orgid=@id AND a.isOrgHouse=1";
        using (var command2 = new MySqlCommand(sql2, connection))
        {
            command2.Parameters.AddWithValue("@id", result[0]["id"]);
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
              housing["House " + count] = row2;
            };
            row["Housing"] = housing;
        }
        return result;
    }
}