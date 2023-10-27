using Application.Contracts;
using Application.Services;

namespace WebAPI.Routes;

public static class ConversationHistoryRoutes
{
    private const string ChatHistoryPolicy = nameof(ChatHistoryPolicy);
    
    public static void MapConversationRoutes(this WebApplication app)
    {
        var routeGroup = app.MapGroup("").WithTags("ConversationHistory").WithOpenApi();
        
        routeGroup
            .MapGet(
                "/Conversation/{id}",
                async (ChatHistoryService historyService, int id) =>
                {
                    var results = historyService.GetConversation(id);
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationById")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapGet(
                "/Conversations",
                async (ChatHistoryService historyService) =>
                {
                    var results = historyService.GetConversations();
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationsForUser")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapPost(
                "/Conversation",
                async (ChatHistoryService historyService, string conversation, string firstMessage) =>
                {
                    var id = await historyService.AddConversation(conversation, firstMessage);
                    return TypedResults.Ok(id);
                })
            .WithName("AddConversationHistory")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapPut(
                "/Conversation{id}", 
                async (ChatHistoryService historyService, int id, string conversation) =>
                {
                    await historyService.UpdateConversation(id, conversation);
                    return TypedResults.Ok();
                })
            .WithName("UpdateConversation")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapDelete(
                "/Conversations",
                async (ChatHistoryService historyService) =>
                {
                    await historyService.ClearAllHistory();
                    return TypedResults.Ok();
                })
            .WithName("DeleteAllConversations")
            .RequireAuthorization(ChatHistoryPolicy);
    }
}