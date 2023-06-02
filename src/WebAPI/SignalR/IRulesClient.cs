using Polly.RateLimit;

namespace WebAPI.SignalR;

public interface IRulesClient
{
    // Methods that a client listens for - connection.on(...)
    Task ReceiveBroadcast(string user, string message);
    Task ReceiveRateLimitedWarning(double retryAfter);
}
