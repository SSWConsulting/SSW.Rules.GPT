using Microsoft.AspNetCore.SignalR;

namespace WebAPI.SignalR;

// Not in use without auth implemented
public class SignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User.Identity?.Name;
    }
}
