using Blazor.Analytics;
using MudBlazor;
using Newtonsoft.Json;
using SharedClasses;
using WebUI.Models;

namespace WebUI.Services;

public class MessagingService
{
    private readonly SignalRClient _signalRClient;
    private readonly RulesGptClient _rulesGptClient;
    private readonly DataState _dataState;
    private readonly UserService _userService;
    private readonly NotifierService _notifierService;
    private readonly IAnalytics _analytics;
    private readonly ISnackbar _snackbar;

    public MessagingService(
        SignalRClient signalRClient,
        RulesGptClient rulesGptClient,
        DataState dataState,
        UserService userService,
        NotifierService notifierService,
        IAnalytics analytics,
        ISnackbar snackbar)
    {
        _dataState = dataState;
        _snackbar = snackbar;
        _userService = userService;
        _rulesGptClient = rulesGptClient;
        _notifierService = notifierService;
        _analytics = analytics;
        _signalRClient = signalRClient;
    }

    public async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_dataState.NewMessageString) || !await CheckConnection())
            return;

        //Create the new message and its response
        var newUserMessage = new ChatMessage("user", _dataState.NewMessageString);
        var newAssistantMessage = new ChatMessage("assistant", string.Empty);

        //Add each to the chat history
        var userLinkedListItem = _dataState.Conversation.ChatList.Any(s => s.Message.Role != "system")
            ? _dataState.Conversation.ChatList.AddAfter(newUserMessage, _dataState.Conversation.ChatList.Last(), _dataState.SelectedGptModel)
            : _dataState.Conversation.ChatList.Add(newUserMessage, _dataState.SelectedGptModel);

        var assistantLinkedListItem = _dataState.Conversation.ChatList.Any(s => s.Message.Role != "system")
            ? _dataState.Conversation.ChatList.AddAfter(newAssistantMessage, _dataState.Conversation.ChatList.Last(), _dataState.SelectedGptModel)
            : _dataState.Conversation.ChatList.Add(newAssistantMessage, _dataState.SelectedGptModel);

        _dataState.Conversation.CurrentThread.Add(userLinkedListItem);
        _dataState.Conversation.CurrentThread.Add(assistantLinkedListItem);
        _dataState.NewMessageString = string.Empty;

        await SendMessageInternal(newAssistantMessage);
    }

    public async Task SendEditedMessage(ChatLinkedListItem item, string message)
    {
        if (!await CheckConnection())
            return;

        var newChatMessage = new ChatMessage("user", message);
        var newAssistantMessage = new ChatMessage("assistant", string.Empty);

        var target = _dataState.Conversation.ChatList.AddRight(newChatMessage, item, _dataState.SelectedGptModel);

        _dataState.Conversation.ChatList.AddAfter(newAssistantMessage, target, _dataState.SelectedGptModel);
        _dataState.Conversation.CurrentThread = _dataState.Conversation.ChatList.GetThread(target);

        await SendMessageInternal(newAssistantMessage);
    }

    private async Task SendMessageInternal(ChatMessage assistantMessage)
    {
        _ = _analytics.TrackEvent("SendMessage", new { message = _dataState.NewMessageString, ownKey = _dataState.UsingByoApiKey });

        _dataState.IsAwaitingResponse = true;
        _dataState.IsAwaitingResponseStream = true;

        await _notifierService.Update();

        var resultStream = _signalRClient.RequestNewCompletionMessage(
            _dataState.Conversation.CurrentThread.Select(s => s.Message).ToList(),
            _dataState.ApiKeyString,
            (OpenAI.GPT3.ObjectModels.Models.Model)_dataState.SelectedGptModel,
            _dataState.CancellationTokenSource.Token);

        await foreach (var result in resultStream)
        {
            _dataState.IsAwaitingResponseStream = false;
            assistantMessage.Content += result?.Content;

            await _notifierService.Update();
        }

        if (_userService.IsUserAuthenticated)
        {
            var serialized = JsonConvert.SerializeObject(
                _dataState.Conversation.ChatList,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.All
                });

            //if (_dataState.Conversation.Id != null)
            //    await _rulesGptClient.UpdateConversationAsync(_dataState.Conversation.Id.Value, serialized);
            //
            //else
                await _rulesGptClient.AddConversationHistoryAsync(serialized, _dataState.Conversation.CurrentThread.FirstOrDefault()?.Message.Content);
        }

        _dataState.IsAwaitingResponseStream = false;
        _dataState.IsAwaitingResponse = false;
        _dataState.CancellationTokenSource.Dispose();
        _dataState.CancellationTokenSource = new CancellationTokenSource();
    }

    private async Task<bool> CheckConnection()
    {
        if (_signalRClient.GetConnectionState() != StatusHubConnectionState.Disconnected)
            return true;

        try
        {
            await _signalRClient.StartAsync(_dataState.CancellationTokenSource.Token);
            return true;
        }

        catch (HttpRequestException)
        {
            _snackbar.Add("Unable to connect to SSW RulesGPT", Severity.Error);
            return false;
        }
    }
}