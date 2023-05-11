using Microsoft.AspNetCore.SignalR;

namespace WebAPI.SignalR;

public class SignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User.Identity?.Name;
    }
}
