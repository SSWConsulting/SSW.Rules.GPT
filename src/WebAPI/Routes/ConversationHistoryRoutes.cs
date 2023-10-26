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
                "/ConversationById",
                async (HttpContext context, int id) =>
                {
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    var results = service.GetConversation(id);
                    
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationById")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapGet(
                "/ConversationsForUser",
                async (HttpContext context) =>
                {
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    var results = service.GetConversations();
                    
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationsForUser")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapPost(
                "/Conversation",
                async (HttpContext context, string conversation, string firstMessage) =>
                {
                    //TODO: Return ID of newly created row to frontend
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.AddConversation(conversation, firstMessage);
                })
            .WithName("AddConversationHistory")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapPut(
                "/Conversation", 
                async (HttpContext context, int id, string conversation) =>
                {
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.UpdateConversation(id, conversation);
                    
                    return TypedResults.Ok();
                })
            .WithName("UpdateConversation")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapDelete(
                "/Conversations",
                async (HttpContext context) =>
                {
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.ClearAllHistory();

                    return TypedResults.Ok();
                })
            .WithName("DeleteAllConversations")
            .RequireAuthorization(ChatHistoryPolicy);
    }
}