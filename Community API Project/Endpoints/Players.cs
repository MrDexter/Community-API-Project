using CommunityApi.Models;
using CommunityApi.Services;
using Microsoft.AspNetCore.Authorization;


namespace CommunityApi.Endpoints;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/players");

        group.MapGet("/", async (int? limit, int? offset, IPlayerService players) =>
        {
            var result = await players.GetAllPlayers(limit, offset);
            return Results.Ok(result);
        });

        group.MapGet("/{id}", async (string id, IPlayerService players) =>
        {
           var result = await players.GetPlayer(id);
           if (result is null)
                return Results.NotFound();
                
            return Results.Ok(result);
        });

        group.MapPost("/{id}/updaterank", [Authorize] async (HttpContext ctx, int id, string rank, string newRank, IPlayerService players, ISecurityService security) =>
        {
            var userName = ctx.User.Identity?.Name ?? "Unknown";
            if (!ctx.User.HasClaim("scope", "write"))
            {
                await security.AuditLog("Access Denied: Rank Update", id, userName, $"{rank} - {newRank}");
                return Results.Forbid();
            };
            var group = ctx.User.FindFirst("side")?.Value;
            string column = group switch
            {
                "police" => rank switch
                {
                    "coplevel" or "tfuLevel" or "ncaLevel" or "npaslevel" or "mpuLevel" or "acadLevel" => rank,
                    _ => null
                },
                "opfor" => rank switch
                {
                    "ionlevel" or "deltalevel" or "UmLevel" or "iaflevel" or "irulevel" => rank,
                    _ => null
                },
                "medic" => rank switch 
                {
                    "mediclevel" or "hemslevel" or "hartlevel" => rank,
                    _ => null
                },
                "staff" => rank switch
                {
                    "adminlevel" or "donorlevel" or "donorexpiry" => rank,
                    _ => null
                },
                //Error
                _ => null
            }; 
            if (column is null) {
                await security.AuditLog("Invalid Rank or Permissions: Rank Update", id, userName, $"{rank} - {newRank}");
                throw new Exception(column);
            };
            var result = await players.UpdateRank(id, column, newRank);
            if (result is null)
            { 
                
                await security.AuditLog("Not Found: Rank Update", id, userName, $"{rank} - {newRank}");   
                return Results.NotFound();
            };
            await security.AuditLog("Complete: Rank Update", id, userName, $"{rank} - {newRank}");
            return Results.Ok(result);
        });
        return app;
    }
}