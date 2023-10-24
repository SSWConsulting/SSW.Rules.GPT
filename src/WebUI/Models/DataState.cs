using SharedClasses;
using WebUI.Services;

namespace WebUI.Models;

public class DataState
{
    private readonly NotifierService _notifierService;

    public DataState(NotifierService notifierService)
    {
        _notifierService = notifierService;
    }

    public ConversationDetails Conversation { get; private set; } = new();
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    
    public string? ApiKeyString { get; set; }
    public string NewMessageString { get; set; } = "";

    public bool UsingByoApiKey => ApiKeyString != null;
    public bool SavingByoApiKey { get; set; }
    public bool IsAwaitingResponse { get; set; }
    public bool IsAwaitingResponseStream { get; set; }
    
    public AvailableGptModels SelectedGptModel { get; set; } =
        (AvailableGptModels)OpenAI.GPT3.ObjectModels.Models.Model.ChatGpt3_5Turbo;

    public async Task CancelStreamingResponse()
    {
        CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();

        var lastAssistantMessage = Conversation.CurrentThread.LastOrDefault(s => s.Message.Role == "assistant");
        if (lastAssistantMessage is not null && string.IsNullOrWhiteSpace(lastAssistantMessage.Message.Content))
        {
            Conversation.CurrentThread.Remove(lastAssistantMessage);
            Conversation.ChatList.Remove(lastAssistantMessage);
        }
        
        IsAwaitingResponse = false;
        IsAwaitingResponseStream = false;
        CancellationTokenSource = new CancellationTokenSource();
    }
    
    public void MoveConversationLeft(ChatLinkedListItem item)
    {
        if (item.Left == null)
            return;
        
        Conversation.ChatList.Move(item, Direction.Left);
        Conversation.CurrentThread = Conversation.ChatList.GetThread(item);

        _ = _notifierService.Update();
    }
    
    public void MoveConversationRight(ChatLinkedListItem item)
    {
        if (item.Right == null)
            return;
        
        Conversation.ChatList.Move(item, Direction.Right);
        Conversation.CurrentThread = Conversation.ChatList.GetThread(item);
        
        _ = _notifierService.Update();
    }
    
    public async Task ResetState()
    {
        await _notifierService.CancelMessageStream();
        Conversation = new ConversationDetails();
        await _notifierService.Update();
    }
}