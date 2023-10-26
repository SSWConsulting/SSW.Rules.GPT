using Application.Services;
using Duende.IdentityServer.Extensions;

namespace WebAPI.Routes;

public static class ConversationHistoryRoutes
{
    public static void MapConversationRoutes(this WebApplication app)
    {
        var routeGroup = app.MapGroup("").WithTags("ConversationHistory").WithOpenApi();
        
        //TODO: Const for policy name
        routeGroup
            .MapGet(
                "/ConversationById",
                async (HttpContext context, int id) =>
                {
                    if (!CheckAuth(context))
                        return null;
                    
                    if (!CheckEmail(context, out var email))
                        return null;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    var results = service.GetConversation(id, email);
                    
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationById")
            .RequireAuthorization("chatHistoryPolicy");
        
        routeGroup
            .MapGet(
                "/ConversationsForUser",
                async (HttpContext context) =>
                {
                    if (!CheckAuth(context))
                        return null;
                    
                    if (!CheckEmail(context, out var email))
                        return null;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    var results = service.GetConversations(email);
                    
                    Console.WriteLine($"Returning {results.Count()} conversations.");
                    
                    return TypedResults.Ok(results);
                })
            .WithName("GetConversationsForUser")
            .RequireAuthorization("chatHistoryPolicy");
        
        routeGroup
            .MapPost(
                "/Conversation",
                async (HttpContext context, string conversation, string firstMessage) =>
                {
                    //TODO: Return ID of newly created row to frontend
                    
                    if (!CheckAuth(context))
                        return;
                    
                    if (!CheckEmail(context, out var email))
                        return;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.AddConversation(email, conversation, firstMessage);
                })
            .WithName("AddConversationHistory")
            .RequireAuthorization("chatHistoryPolicy");
        
        routeGroup
            .MapPut(
                "/Conversation", 
                async (HttpContext context, int id, string conversation) =>
                {
                    if (!CheckAuth(context))
                        return;
                    
                    if (!CheckEmail(context, out var email))
                        return;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.UpdateConversation(id, email, conversation);
                })
            .WithName("UpdateConversation")
            .RequireAuthorization("chatHistoryPolicy");;
        
        routeGroup
            .MapDelete(
                "/Conversations/{id}",
                async (HttpContext context, int id) =>
                {
                    if (!CheckAuth(context))
                        return;
        
                    if (!CheckEmail(context, out var email))
                        return;
        
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    await service.DeleteConversation(id, email);
                })
            .WithName("DeleteConversation")
            .RequireAuthorization("chatHistoryPolicy");
        
        routeGroup
            .MapDelete(
                "/Conversations",
                async (HttpContext context) => { return await Bar(context); })
            .WithName("DeleteAllConversations")
            .RequireAuthorization("chatHistoryPolicy");
    }

    private static async Task<IResult> Bar(HttpContext context)
    {
        if (!CheckAuth(context))
            return TypedResults.Forbid();

        if (!CheckEmail(context, out var email))
            return TypedResults.Forbid();

        var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
        await service.ClearAllHistory(email);

        return TypedResults.Ok();
    }

    // TODOD: Test this can be removed
    private static bool CheckAuth(HttpContext context)
    {
        if (context.User.IsAuthenticated())
            return true;
        
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }

    private static bool CheckEmail(HttpContext context, out string email)
    {
        email = context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? "";
        return !string.IsNullOrWhiteSpace(email);
    }
}