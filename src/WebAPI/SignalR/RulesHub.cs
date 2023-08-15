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
    private readonly MessageHandler _messageHandler;
    private readonly ILogger<RulesHub> _logger;
    private readonly IOpenAiEmbeddingService _embeddingService;
    private readonly IOpenAiChatCompletionsService _chatCompletionsService;

    public RulesHub(MessageHandler messageHandler, ILogger<RulesHub> logger, IOpenAiEmbeddingService embeddingService, IOpenAiChatCompletionsService chatCompletionsService)
    {
        _messageHandler = messageHandler;
        _logger = logger;
        _embeddingService = embeddingService;
        _chatCompletionsService = chatCompletionsService;

        _embeddingService.OnRateLimited = OnRateLimited;
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
    public async Task BroadcastMessage(string user, string message)
    {
        await Clients.All.ReceiveBroadcast(user, message);
    }

    public IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey,
        Models.Model gptModel,
        CancellationToken cancellationToken
    )
    {
        if (Context.User.IsAuthenticated())
        {
            //TODO: Track user stats
        }
        
        return _messageHandler.Handle(messageList, apiKey, gptModel, cancellationToken);
    }
}