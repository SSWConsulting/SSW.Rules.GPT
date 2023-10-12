using Application.Services;
using Duende.IdentityServer.Extensions;

namespace WebAPI.Routes;

public static class ConversationHistoryRoutes
{
    public static void MapConversationRoutes(this WebApplication app)
    {
        var routeGroup = app.MapGroup("").WithTags("ConversationHistory");
        
        routeGroup
            .MapGet(
                "/getConversationById",
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
                "/getConversationByUser",
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
            .WithName("GetConversationByUser")
            .RequireAuthorization("chatHistoryPolicy");

        routeGroup
            .MapPost(
                "/addConversation",
                async (HttpContext context, string conversation) =>
                {
                    if (!CheckAuth(context))
                        return;
                    
                    if (!CheckEmail(context, out var email))
                        return;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    service.AddConversation(email, conversation);
                })
            .WithName("AddConversationHistory")
            .RequireAuthorization("chatHistoryPolicy");
        
        routeGroup
            .MapPost(
                "/updateConversation", 
                async (HttpContext context, int id, string conversation) =>
                {
                    if (!CheckAuth(context))
                        return;
                    
                    if (!CheckEmail(context, out var email))
                        return;
                    
                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    service.UpdateConversation(id, email, conversation);
                })
            .WithName("UpdateConversation")
            .RequireAuthorization("chatHistoryPolicy");;

        routeGroup
            .MapPost(
                "/deleteConversation",
                async (HttpContext context, int id) =>
                {
                    if (!CheckAuth(context))
                        return;

                    if (!CheckEmail(context, out var email))
                        return;

                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    service.DeleteConversation(id, email);
                })
            .WithName("DeleteConversation")
            .RequireAuthorization("chatHistoryPolicy");
        
        routeGroup
            .MapGet(
                "/clearAllHistory",
                async context =>
                {
                    if (!CheckAuth(context))
                        return;

                    if (!CheckEmail(context, out var email))
                        return;

                    var service = context.RequestServices.GetRequiredService<ChatHistoryService>();
                    service.ClearAllHistory(email);
                })
            .WithName("ClearAllHistory")
            .RequireAuthorization("chatHistoryPolicy");
    }

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