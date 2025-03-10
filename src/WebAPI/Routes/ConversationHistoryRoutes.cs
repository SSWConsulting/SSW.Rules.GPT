using Application.Contracts;
using Application.Services;

namespace WebAPI.Routes;

public static class ConversationHistoryRoutes
{
    public static void MapConversationRoutes(this RouteGroupBuilder app)
    {
        var routeGroup = app.MapGroup("").WithTags("ConversationHistory").WithOpenApi();

        routeGroup
            .MapGet(
                "/Conversation/{id}",
                async (ChatHistoryService historyService, int id) =>
                {
                    var results = await historyService.GetConversation(id);
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationById");
        routeGroup
            .MapGet(
                "/Conversations",
                async (ChatHistoryService historyService) =>
                {
                    var results = await historyService.GetConversations();
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationsForUser");

        routeGroup
            .MapPost(
                "/Conversation",
                async (ChatHistoryService historyService, string conversation, string firstMessage) =>
                {
                    var id = await historyService.AddConversation(conversation, firstMessage);
                    return TypedResults.Ok(id);
                })
            .WithName("AddConversationHistory");

        routeGroup
            .MapPut(
                "/Conversation/{id}",
                async (ChatHistoryService historyService, int id, string conversation) =>
                {
                    await historyService.UpdateConversation(id, conversation);
                    return TypedResults.Ok();
                })
            .WithName("UpdateConversation");

        routeGroup
            .MapDelete(
                "/Conversations",
                async (ChatHistoryService historyService) =>
                {
                    await historyService.ClearAllHistory();
                    return TypedResults.Ok();
                })
            .WithName("DeleteAllConversations");
    }
}