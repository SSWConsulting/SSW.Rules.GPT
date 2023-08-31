using Application.Contracts;
using Application.Services;
using Domain.Entities;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.SignalR;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Polly.RateLimit;

namespace WebAPI.SignalR;

public class RulesHub : Hub<IRulesClient>
{
    private readonly ILogger<RulesHub> _logger;
    private readonly IRulesContext _rulesContext;
    private readonly MessageHandler _messageHandler;

    public RulesHub(
        MessageHandler messageHandler,
        ILogger<RulesHub> logger,
        IOpenAiEmbeddingService embeddingService, 
        IRulesContext rulesContext)
    {
        _messageHandler = messageHandler;
        _logger = logger;
        _rulesContext = rulesContext;

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
            var claims = Context.User?.Claims.ToList();
            if (claims == null || !claims.Any())
            {
                _logger.LogError("Failed to read claims on an authenticated user.");
            }
            
            else
            {
                var email = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                var name = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value;
                var surname = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value;

                if (email == null || name == null || surname == null)
                {
                    _logger.LogError("Failed to read claims on an authenticated user.");
                }
                
                else
                {
                    _ = TrackUserMessage(email, $"{name} {surname}");
                }
            }
        }

        //Check user has a key if they are not signed in and are trying to access GPT-4
        else if (!isAuthenticated && string.IsNullOrWhiteSpace(apiKey) && gptModel == Models.Model.Gpt_4)
        {
            Clients.Caller.ReceiveInvalidModelWarning();
            gptModel = Models.Model.ChatGpt3_5Turbo;
        }

        return _messageHandler.Handle(messageList, apiKey, gptModel, cancellationToken);
    }

    private async Task TrackUserMessage(string email, string name)
    {
        _rulesContext.UserStats.Add(new LeaderboardModel()
        {
            Date = DateTimeOffset.Now.UtcDateTime,
            Email = email,
            Name = name
        });

        await _rulesContext.SaveChangesAsync();
    }
}