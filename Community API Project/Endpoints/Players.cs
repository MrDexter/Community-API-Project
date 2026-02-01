using CommunityApi.Models;
using CommunityApi.Services;
using Microsoft.AspNetCore.Authorization;


namespace CommunityApi.Endpoints;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/players");

        group.MapGet("/", async (IPlayerService players) =>
        {
            var result = await players.GetAllPlayers();
            return Results.Ok(result);
        });

        group.MapGet("/{id}", async (string id, IPlayerService players) =>
        {
           var result = await players.GetPlayer(id);
           if (result is null)
                return Results.NotFound();
                
            return Results.Ok(result);
        });

        group.MapPost("/{id}/updaterank", [Authorize] async (HttpContext ctx, int id, string rank, string newRank, IPlayerService players) =>
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
            var result = await players.UpdateRank(id, column, newRank);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });
        return app;
    }
}