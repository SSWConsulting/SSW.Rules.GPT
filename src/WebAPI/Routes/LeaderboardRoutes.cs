using Application.Contracts;
using Domain;
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
                    var rulesContext = context.RequestServices.GetRequiredService<IRulesContext>();

                    //Needs to be universal time in order to work the database date values
                    var oneMonth = DateTime.Now.AddMonths(-1).ToUniversalTime();
                    var oneYear = DateTime.Now.AddYears(-1).ToUniversalTime();

                    var result = rulesContext.UserStats
                        .GroupBy(s => new { s.Name, s.Email })
                        .Select(group => new LeaderboardUser()
                        {
                            Name = group.Key.Name,
                            Email = group.Key.Email,
                            LastMonth = group.Count(u => u.Date >= oneMonth),
                            LastYear = group.Count(u => u.Date >= oneYear),
                            AllTime = group.Count()
                        })
                        .AsEnumerable();
                    
                    return TypedResults.Ok(result);
                }
            )
            .WithName("GetLeaderboardStats");
    }
}