using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using OpenAI.GPT3.ObjectModels.RequestModels;
using WebUI.Services;

namespace WebUI;

public class SignalRClient
{
    private readonly HubConnection _connection;
    private readonly NotifierService _notifierService;

    public SignalRClient(
        NotifierService notifierService,
        IConfiguration configuration,
        ILogger<SignalRClient> logger,
        IAccessTokenProvider tokenProvider)
    {
        _notifierService = notifierService;

        var hubeBaseUrl = configuration["ApiBaseUrl"];
        var hubUrl = $"{hubeBaseUrl}/ruleshub";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var tokenResult = await tokenProvider.RequestAccessToken();
                    if (tokenResult.TryGetToken(out var token))
                        return await Task.FromResult(token.Value);

                    return await Task.FromResult("");
                };
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        _connection.Closed += async exception =>
        {
            if (exception != null)
                logger.LogInformation("Connection closed due to an error: {Exception}", exception);
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

    public StatusHubConnectionState GetConnectionState()
    {
        var state = _connection.State switch
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

    public async IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey,
        OpenAI.GPT3.ObjectModels.Models.Model gptModel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var completionResult = _connection.StreamAsync<ChatMessage?>(
            "RequestNewCompletionMessage",
            messageList,
            apiKey,
            gptModel,
            cancellationToken
        );

        await foreach (var message in completionResult)
            yield return message;
    }

    //Methods that the client listens for from the server

    private void RegisterHandlers()
    {
        _connection.On<double>("ReceiveRateLimitedWarning", async retryAfter => { await _notifierService.RaiseRateLimited(retryAfter); });
        _connection.On("ReceiveInvalidModelWarning", async () => { await _notifierService.RaiseInvalidModel(); });
    }
}