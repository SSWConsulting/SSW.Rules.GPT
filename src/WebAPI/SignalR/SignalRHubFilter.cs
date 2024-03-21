using Microsoft.AspNetCore.SignalR;

namespace WebAPI.SignalR;

public class SignalRHubFilter : IHubFilter
{
    private readonly ILogger<RulesHub> _logger;

    public SignalRHubFilter(ILogger<RulesHub> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next
    )
    {
        _logger.LogInformation(
            "Calling hub method '{HubMethodName}'",
            invocationContext.HubMethodName
        );
        try
        {
            return await next(invocationContext);
        }
        // This doesn't work for IAsyncEnumerable
        // TODO: Catch exceptions from IAsyncEnumerable
        catch (Exception ex)
        {
            _logger.LogError(
                "Exception calling '{HubMethodName}': {ex}",
                invocationContext.HubMethodName,
                ex
            );
            throw;
        }
    }
}
