using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using OpenAI.GPT3.ObjectModels.RequestModels;
using WebUI.Models;
using WebUI.Services;
using Polly.RateLimit;

namespace WebUI;

public class SignalRClient 
{
    private readonly DataState _dataState;
    private readonly NotifierService _notifierService;
    private readonly HubConnection _connection;
    private readonly ILogger<SignalRClient> _logger;

    public SignalRClient(
        DataState dataState,
        IWebAssemblyHostEnvironment hostEnvironment,
        NotifierService notifierService, 
        ILogger<SignalRClient> logger)
    {
        _dataState = dataState;
        _notifierService = notifierService;
        _logger = logger;
        var hubeBaseUrl = hostEnvironment.IsDevelopment()
            ? "https://localhost:7104"
            : "https://ssw-rulesgpt-api.azurewebsites.net";
        var hubUrl = $"{hubeBaseUrl}/ruleshub";
        _connection = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();
        RegisterHandlers();
        _connection.Closed += async (exception) =>
        {
            if (exception != null)
            {
                _logger.LogInformation("Connection closed due to an error: {Exception}", exception);        
            }
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _connection.StartAsync(cancellationToken);
    }

    public async Task StopAsync()
    {
        await _connection.StopAsync();
    }

    //TODO: Refactor to separate file
    public enum StatusHubConnectionState
    {
        Disconnected,
        Connected,
        Connecting,
        Reconnecting
    }

    public StatusHubConnectionState GetConnectionState()
    {
        StatusHubConnectionState state;
        state = _connection.State switch
        {
            HubConnectionState.Disconnected => StatusHubConnectionState.Disconnected,
            HubConnectionState.Connected => StatusHubConnectionState.Connected,
            HubConnectionState.Connecting => StatusHubConnectionState.Connecting,
            HubConnectionState.Reconnecting => StatusHubConnectionState.Reconnecting,
            _ => throw new ArgumentOutOfRangeException()
        };
        return state;
    }

    // Methods the client can call on the server


    public async Task BroadcastMessageAsync(string userName, string message)
    {
        await _connection.InvokeAsync("BroadcastMessage", userName, message);
    }

    public async IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey,
        OpenAI.GPT3.ObjectModels.Models.Model gptModel,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var completionResult = _connection.StreamAsync<ChatMessage?>(
            "RequestNewCompletionMessage",
            messageList,
            apiKey,
            gptModel,
            cancellationToken
        );
        await foreach (var message in completionResult.WithCancellation(cancellationToken))
        {
            yield return message;
        }
    }
    
    //Methods that the client listens for
    private void RegisterHandlers()
    {
        _connection.On<string, string>(
            "ReceiveBroadcast",
            (user, message) =>
            {
                var encodedMsg = $"{user}: {message}";
                Console.WriteLine(encodedMsg);
            }
        );
        _connection.On<double>("ReceiveRateLimitedWarning", async (retryAfter) => { await _notifierService.RaiseRateLimited(retryAfter); });
    }
}
