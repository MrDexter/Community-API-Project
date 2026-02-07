using MySqlConnector;
using CommunityApi.Models;

namespace CommunityApi.Services;

public interface ISecurityService
{
    Task AuditLog(string type, int? id, string performedBy, string details);   
}

public class SecurityService : ISecurityService
{
    private readonly string connectionString;
    private readonly ILogger<SecurityService> _logger;
    public SecurityService(IConfiguration config, ILogger<SecurityService> logger)
    {
        connectionString = config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing Default Connection");
        _logger = logger;
    }

    public async Task AuditLog(string type, int? id, string performedBy, string details)
    {
        using var connection = new MySqlConnection(connectionString);
        try 
        {
            await connection.OpenAsync();
            var sql = @"Insert INTO audit_log (event_type, player_id, performed_by, details) VALUES (@type, @id, @performedBy, @details)";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@type", type);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@performedBy", performedBy);
            command.Parameters.AddWithValue("@details", details);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Audit Insert Failed! {type} Request by {user} for Player: {playerid} Rank: {details}", type, performedBy, id, details);
        } 
    }
}