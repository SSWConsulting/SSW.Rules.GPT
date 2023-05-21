using Application.Services;
using Microsoft.AspNetCore.SignalR;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebAPI.SignalR;

public class RulesHub : Hub<IRulesClient>
{
    private readonly MessageHandler _messageHandler;
    private readonly ILogger<RulesHub> _logger;

    public RulesHub(MessageHandler messageHandler, ILogger<RulesHub> logger)
    {
        _messageHandler = messageHandler;
        _logger = logger;
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
        return _messageHandler.Handle(messageList, apiKey, gptModel, cancellationToken);
    }
}
