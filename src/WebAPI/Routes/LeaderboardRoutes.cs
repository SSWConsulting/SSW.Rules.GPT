using Application.Contracts;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI.Routes;

public static class LeaderboardRoutes
{
    public static void MapLeaderboardRoutes(this WebApplication app)
    {
        var group = app.MapGroup("").WithTags("Leaderboard");

        group
            .MapGet(
                "/getLeaderboardStats",
                Ok<IEnumerable<LeaderboardUser>> (HttpContext context) =>
                {
                    var rulesContext = context.RequestServices.GetRequiredService<IRulesContext>();

                    var result = rulesContext.UserStats
                        .Select(s => 
                        new LeaderboardUser()
                        {
                            Name = s.Name,
                            Email = s.Email,
                            LastMonth = rulesContext.UserStats.Count(u => u.Email == s.Email && u.Date >= DateTime.Now.AddMonths(-1)),
                            LastYear = rulesContext.UserStats.Count(u => u.Email == s.Email && u.Date >= DateTime.Now.AddYears(-1)),
                            AllTime = rulesContext.UserStats.Count(u => u.Email == s.Email)
                        })
                        .Distinct()
                        .AsEnumerable();
                    
                    return TypedResults.Ok(result);
                }
            )
            .WithName("GetLeaderboardStats");
    }
}