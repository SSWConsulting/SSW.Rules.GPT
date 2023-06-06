using Blazor.Analytics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using OpenAI.GPT3.ObjectModels.RequestModels;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Components;

public class RulesBotChatBase : ComponentBase, IDisposable
{
    [Inject] protected DataState DataState { get; set; } = default!;
    [Inject] protected SswRulesGptDialogService SswRulesGptDialogService { get; set; } = default!;
    [Inject] protected ApiKeyValidationService ApiKeyValidationService { get; set; } = default!;
    [Inject] protected NotifierService NotifierService { get; set; } = default!;
    [Inject] protected SignalRClient SignalR { get; set; } = default!;
    [Inject] protected IJSRuntime Js { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IAnalytics Analytics { get; set; } = default!;
    
    [CascadingParameter] protected MudTheme Theme { get; set; }
    [CascadingParameter] protected bool isDarkMode { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        NotifierService.Notify += OnNotify;
        NotifierService.CancelMessageStreamEvent += OnCancelMessageStream;
    }

    protected async Task OnExampleClicked(string message)
    {
        DataState.NewMessageString = message;
        await SendMessage();
    }

    protected async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(DataState.NewMessageString))
        {
            return;
        }

        if (SignalR.GetConnectionState() == SignalRClient.StatusHubConnectionState.Disconnected)
        {
            try
            {
                await SignalR.StartAsync(DataState.CancellationTokenSource.Token);
            }
            catch (HttpRequestException e)
            {
                Snackbar.Add("Unable to connect to SSW RulesGPT", Severity.Error);
                return;
            }
        }

        _ = Analytics.TrackEvent("SendMessage", new { message = DataState.NewMessageString, ownKey = DataState.OpenAiApiKey != null });

        var newChatMessage = new ChatMessage("user", DataState.NewMessageString);
        DataState.NewMessageString = string.Empty;
        DataState.ChatMessages.Add(newChatMessage);

        var newAssistantMessage = new ChatMessage("assistant", string.Empty);

        DataState.ChatMessages.Add(newAssistantMessage);
        DataState.IsAwaitingResponseStream = true;
        DataState.IsAwaitingResponse = true;
        StateHasChanged();

        await JsScrollMessageListToBottom();

        var resultStream = SignalR.RequestNewCompletionMessage(
            DataState.ChatMessages,
            DataState.OpenAiApiKey,
            DataState.SelectedGptModel,
            DataState.CancellationTokenSource.Token);

        await JsScrollMessageListToBottom();
        await foreach (var result in resultStream.WithCancellation(DataState.CancellationTokenSource.Token))
        {
            DataState.IsAwaitingResponseStream = false;
            newAssistantMessage.Content += result?.Content;

            StateHasChanged();
            _= NotifierService.Update();
            await Js.InvokeVoidAsync("highlightCode");
            await JsScrollMessageListToBottom();
        }
        DataState.IsAwaitingResponseStream = false;
        DataState.IsAwaitingResponse = false;
        DataState.CancellationTokenSource.Dispose();
        DataState.CancellationTokenSource = new CancellationTokenSource();
    }

    protected async Task MessageTextFieldHandleEnterKey(KeyboardEventArgs args)
    {
        if (args is { Key: "Enter", ShiftKey: false })
        {
            await SendMessage();
        }
    }

    private async Task OnNotify()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnCancelMessageStream()
    {
        await InvokeAsync(CancelStreamingResponse);
    }
    
    public void Dispose()
    {
        NotifierService.Notify -= OnNotify;
        NotifierService.CancelMessageStreamEvent -= OnCancelMessageStream;
    }

    private async Task JsScrollMessageListToBottom()
    {
        await Js.InvokeVoidAsync("scrollLatestMessageIntoView");
    }

    private async Task CancelStreamingResponse()
    {
        DataState.CancellationTokenSource.Cancel();
        DataState.CancellationTokenSource.Dispose();
        var lastAssistantMessage = DataState.ChatMessages.LastOrDefault(s => s.Role == "assistant");
        if (lastAssistantMessage is not null && string.IsNullOrWhiteSpace(lastAssistantMessage?.Content))
        {
            DataState.ChatMessages.Remove(lastAssistantMessage);
        }
        DataState.IsAwaitingResponse = false;
        DataState.IsAwaitingResponseStream = false;
        DataState.CancellationTokenSource = new CancellationTokenSource();
    }
}