using MySqlConnector;
using CommunityApi.Models;

namespace CommunityApi.Services;

public interface IPlayerService
{
    Task<List<Player>> GetAllPlayers(int? limit, int? offset);
    Task<List<Dictionary<string, object>>> GetPlayer(string id);
    Task<UpdateRank> UpdateRank(int id, string rank, string newRank);
}

public class PlayerService : IPlayerService
{
    private readonly string connectionString;

    public PlayerService(IConfiguration config)
    {
        connectionString = config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing Default Connection");
    }

    public async Task<List<Player>> GetAllPlayers(int? limit, int? offset)
    {
        var result = new List<Player>();
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var sql = "Select uid, name, playerid, cash, bankacc, cartelCredits, adminLevel, copLevel, ionLevel, medicLevel, last_seen, insert_time From players";
            if (limit.HasValue)
            {
                sql += " LIMIT @limit";
                if (offset.HasValue)
                {
                    sql += " OFFSET @offset";  
                };
            };
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@limit", limit);
            command.Parameters.AddWithValue("@offset", offset);
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
        return result;
    }

    public async Task<List<Dictionary<string, object>>> GetPlayer(string id)
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
                return null;
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
        return result; 
    }

    public async Task<UpdateRank> UpdateRank(int id, string column, string newRank)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        UpdateRankGet? oldData = null;
        var sqlGet = $"Select name, {column} FROM players WHERE uid = @id";
        using (var getCommand = new MySqlCommand(sqlGet, connection))
        {
            getCommand.Parameters.AddWithValue("@rank", column);
            getCommand.Parameters.AddWithValue("@id", id);
            using var reader = await getCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            };
            
            oldData = new UpdateRankGet(
                reader["name"].ToString() ?? string.Empty,
                reader[column].ToString() ?? string.Empty
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
                column.ToString() ?? string.Empty,
                oldRank.ToString() ?? string.Empty,
                newRank.ToString() ?? string.Empty,
                reader > 0 ? "Success" : "Failed"
            );
            return result; 
        };
    }

}