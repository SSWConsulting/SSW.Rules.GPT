using Blazor.Analytics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using OpenAI.GPT3.ObjectModels.RequestModels;
using WebUI.Classes;
using WebUI.Models;
using WebUI.Services;
using Direction = WebUI.Classes.Direction;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        
        if (firstRender)
        {
            await Js.InvokeVoidAsync("initInputHeight");
        }
    }
    
    protected async Task OnExampleClicked(string message)
    {
        DataState.NewMessageString = message;
        await SendMessage();
    }

    protected async Task Move((ChatLinkedListItem item, Direction direction) args)
    {
        var target = args.direction == Direction.Left 
            ? args.item.Left
            : args.item.Right;
        
        if (target is null)
            return;
        
        DataState.ChatMessages.Move(args.item, args.direction);
        DataState.CurrentMessageThread = DataState.ChatMessages.GetThread(target);
        StateHasChanged();
    }

    protected async Task SendEditedMessage((ChatLinkedListItem item, string message) args)
    {
        if (!await CheckConnection())
        {
            return;
        }
        
        var newChatMessage = new ChatMessage("user", args.message);
        var newAssistantMessage = new ChatMessage("assistant", string.Empty);
        
        var target = DataState.ChatMessages.AddRight(newChatMessage, args.item, DataState.SelectedGptModel);
        
        DataState.ChatMessages.AddAfter(newAssistantMessage, target, DataState.SelectedGptModel);
        DataState.CurrentMessageThread = DataState.ChatMessages.GetThread(target);
        
        await SendMessage(newChatMessage, newAssistantMessage);
    }

    protected async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(DataState.NewMessageString) || !await CheckConnection())
        {
            return;
        }
        
        var newChatMessage = new ChatMessage("user", DataState.NewMessageString);
        var newAssistantMessage = new ChatMessage("assistant", string.Empty);

        var userLinkedListItem = DataState.ChatMessages.Any(s => s.Message.Role != "system") 
            ? DataState.ChatMessages.AddAfter(newChatMessage, DataState.ChatMessages.Last(), DataState.SelectedGptModel)
            : DataState.ChatMessages.Add(newChatMessage, DataState.SelectedGptModel);
        
        var assistantLinkedListItem = DataState.ChatMessages.Any(s => s.Message.Role != "system") 
            ? DataState.ChatMessages.AddAfter(newAssistantMessage, DataState.ChatMessages.Last(), DataState.SelectedGptModel)
            : DataState.ChatMessages.Add(newAssistantMessage, DataState.SelectedGptModel);
        
        DataState.CurrentMessageThread.Add(userLinkedListItem);
        DataState.CurrentMessageThread.Add(assistantLinkedListItem);
        DataState.NewMessageString = string.Empty;
        
        await SendMessage(newChatMessage, newAssistantMessage);
    }

    private async Task SendMessage(ChatMessage chatMessage, ChatMessage assistantMessage)
    {
        _ = Analytics.TrackEvent("SendMessage", new { message = DataState.NewMessageString, ownKey = DataState.OpenAiApiKey != null });
        
        DataState.IsAwaitingResponseStream = true;
        DataState.IsAwaitingResponse = true;
        
        StateHasChanged();

        await JsScrollMessageListToBottom();
        await Js.InvokeVoidAsync("setInputHeight");

        var resultStream = SignalR.RequestNewCompletionMessage(
            DataState.CurrentMessageThread.Select(s => s.Message).ToList(),
            DataState.OpenAiApiKey,
            (OpenAI.GPT3.ObjectModels.Models.Model)DataState.SelectedGptModel,
            DataState.CancellationTokenSource.Token);

        await JsScrollMessageListToBottom();
        await foreach (var result in resultStream.WithCancellation(DataState.CancellationTokenSource.Token))
        {
            DataState.IsAwaitingResponseStream = false;
            assistantMessage.Content += result?.Content;

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

    private async Task<bool> CheckConnection()
    {
        if (SignalR.GetConnectionState() == SignalRClient.StatusHubConnectionState.Disconnected)
        {
            try
            {
                await SignalR.StartAsync(DataState.CancellationTokenSource.Token);
            }
            catch (HttpRequestException e)
            {
                Snackbar.Add("Unable to connect to SSW RulesGPT", Severity.Error);
                return false;
            }
        }

        return true;
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

    protected async Task CancelStreamingResponse()
    {
        DataState.CancellationTokenSource.Cancel();
        DataState.CancellationTokenSource.Dispose();
        
        var lastAssistantMessage = DataState.CurrentMessageThread.LastOrDefault(s => s.Message.Role == "assistant");
        if (lastAssistantMessage is not null && string.IsNullOrWhiteSpace(lastAssistantMessage?.Message.Content))
        {
            DataState.CurrentMessageThread.Remove(lastAssistantMessage);
            DataState.ChatMessages.Remove(lastAssistantMessage);
        }
        
        DataState.IsAwaitingResponse = false;
        DataState.IsAwaitingResponseStream = false;
        DataState.CancellationTokenSource = new CancellationTokenSource();
    }
}