using CommunityApi.Models;
using CommunityApi.Services;

namespace CommunityApi.Endpoints;

public static class GangEndpoints
{
    public static IEndpointRouteBuilder MapGangEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/gangs");

        group.MapGet("/", async (IGangService gangs) =>
        {
            var result = await gangs.GetAllGangs();
            return Results.Ok(result);
        });

        group.MapGet("/{id}", async (string id, IGangService gangs) =>
        {
            var result = await gangs.GetGang(id);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        });

        return app;
    }
}