namespace WebAPI.SignalR;

public interface IRulesClient
{
    // Methods that a client listens for - connection.on(...)
    Task ReceiveRateLimitedWarning(double retryAfter);

    Task ReceiveInvalidModelWarning();
}