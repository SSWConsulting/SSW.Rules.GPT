using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI.Routes;

public static class LeaderboardRoutes
{
    public static void MapLeaderboardRoutes(this WebApplication app)
    {
        var routeGroup = app.MapGroup("").WithTags("Leaderboard");

        routeGroup
            .MapGet(
                "/getLeaderboardStats",
                Ok<IEnumerable<LeaderboardUser>> (HttpContext context) =>
                {
                    var service = context.RequestServices.GetRequiredService<LeaderboardService>();
                    return TypedResults.Ok(service.GetLeaderboardStats());
                }
            )
            .WithName("GetLeaderboardStats");
    }
}