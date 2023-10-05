using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI.Routes;

public static class ConversationHistoryRoutes
{
    public static void MapConversationRoutes(this WebApplication app)
    {
        var routeGroup = app.MapGroup("").WithTags("ConversationHistory");

        routeGroup
            .MapGet(
                "/getHistory",
                Ok<IEnumerable<LeaderboardUser>> (HttpContext context) =>
                {
                    var service = context.RequestServices.GetRequiredService<LeaderboardService>();
                    return TypedResults.Ok(service.GetLeaderboardStats());
                }
            )
            .WithName("GetConversationHistory");

        routeGroup
            .MapPost(
                "/addHistory",
                async (HttpRequest http, string chatHistory) =>
                {
                    Console.WriteLine("called");
                    Console.WriteLine(chatHistory);
                })
            .WithName("AddConversationHistory")
            .RequireAuthorization("chatHistoryPolicy");
    }
}