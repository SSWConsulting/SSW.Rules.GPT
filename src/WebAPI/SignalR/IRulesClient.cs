using Domain;

namespace WebAPI.SignalR;

public interface IRulesClient
{
    // Methods that a client listens for - connection.on(...)
    Task ReceiveBroadcast(string user, string message);
}
