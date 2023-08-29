using Application.Contracts;
using Application.Services;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.SignalR;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Polly.RateLimit;

namespace WebAPI.SignalR;

public class RulesHub : Hub<IRulesClient>
{
    private readonly ILogger<RulesHub> _logger;
    private readonly MessageHandler _messageHandler;

    public RulesHub(
        MessageHandler messageHandler,
        ILogger<RulesHub> logger,
        IOpenAiEmbeddingService embeddingService)
    {
        _messageHandler = messageHandler;
        _logger = logger;

        embeddingService.OnRateLimited = OnRateLimited;
    }

    // override OnConnectedAsync to add user to group
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("User connected: {User}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            _logger.LogInformation("User disconnected: {User}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        _logger.LogInformation(
            "User disconnected: {User} with error: {Error}",
            Context.ConnectionId,
            exception.Message
        );
        return base.OnConnectedAsync();
    }

    public async Task OnRateLimited(RateLimitRejectedException e)
    {
        var retryAfter = Math.Round(e.RetryAfter.TotalSeconds, MidpointRounding.AwayFromZero);
        await Clients.Caller.ReceiveRateLimitedWarning(retryAfter);
    }

    // Server methods that a client can invoke - connection.invoke(...)

    public IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey,
        Models.Model gptModel,
        CancellationToken cancellationToken
    )
    {
        var isAuthenticated = Context.User.IsAuthenticated();
        if (isAuthenticated)
        {
            //TODO: Track user stats - see https://github.com/SSWConsulting/SSW.Rules.GPT/issues/103
        }

        //Check user has a key if they are not signed in and are trying to access GPT-4
        else if (!isAuthenticated && string.IsNullOrWhiteSpace(apiKey) && gptModel == Models.Model.Gpt_4)
        {
            Clients.Caller.ReceiveInvalidModelWarning();
            gptModel = Models.Model.ChatGpt3_5Turbo;
        }

        return _messageHandler.Handle(messageList, apiKey, gptModel, cancellationToken);
    }
}