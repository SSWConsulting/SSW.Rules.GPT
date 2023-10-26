using Application.Services;
using Duende.IdentityServer.Extensions;

namespace WebAPI.Routes;

public static class ConversationHistoryRoutes
{
    private const string ChatHistoryPolicy = "chatHistoryPolicy";
    
    public static void MapConversationRoutes(this WebApplication app)
    {
        var routeGroup = app.MapGroup("").WithTags("ConversationHistory").WithOpenApi();
        
        routeGroup
            .MapGet(
                "/ConversationById",
                async (HttpContext context, int id) =>
                {
                    if (!CheckEmail(context, out var email))
                        return null;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    var results = service.GetConversation(id, email);
                    
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationById")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapGet(
                "/ConversationsForUser",
                async (HttpContext context) =>
                {
                    if (!CheckEmail(context, out var email))
                        return null;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    var results = service.GetConversations(email);
                    
                    Console.WriteLine($"Returning {results.Count()} conversations.");
                    
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
                    
                    if (!CheckEmail(context, out var email))
                        return;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.AddConversation(email, conversation, firstMessage);
                })
            .WithName("AddConversationHistory")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapPut(
                "/Conversation", 
                async (HttpContext context, int id, string conversation) =>
                {
                    if (!CheckEmail(context, out var email))
                        return;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.UpdateConversation(id, email, conversation);
                })
            .WithName("UpdateConversation")
            .RequireAuthorization(ChatHistoryPolicy);
        
        routeGroup
            .MapDelete(
                "/Conversations",
                async (HttpContext context) => { return await Bar(context); })
            .WithName("DeleteAllConversations")
            .RequireAuthorization(ChatHistoryPolicy);
    }

    private static async Task<IResult> Bar(HttpContext context)
    {
        if (!CheckEmail(context, out var email))
            return TypedResults.Forbid();

        var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
        await service.ClearAllHistory(email);

        return TypedResults.Ok();
    }

    private static bool CheckEmail(HttpContext context, out string email)
    {
        email = context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? "";
        return !string.IsNullOrWhiteSpace(email);
    }
}