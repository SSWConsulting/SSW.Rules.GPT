using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI.Routes;

public static class LeaderboardRoutes
{
    public static void MapLeaderboardRoutes(this RouteGroupBuilder app)
    {
        var routeGroup = app.MapGroup("leaderboard").WithTags("Leaderboard");

        routeGroup
            .MapGet(
                "/getLeaderboardStats",
                async Task<Ok<List<LeaderboardUser>>> (LeaderboardService leaderboardService) =>
                {
                    return TypedResults.Ok(await leaderboardService.GetLeaderboardStats());
                }
            )
            .WithName("GetLeaderboardStats");
    }
}