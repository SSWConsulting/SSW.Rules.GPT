using OpenAI.GPT3.ObjectModels.RequestModels;
using WebUI.Services;

namespace WebUI.Models;

public class DataState
{
    private readonly NotifierService _notifierService;
    
    public DataState(NotifierService notifierService)
    {
        _notifierService = notifierService;
    }
    
    public string? OpenAiApiKey { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
    public OpenAI.GPT3.ObjectModels.Models.Model SelectedGptModel { get; set; } =
        OpenAI.GPT3.ObjectModels.Models.Model.ChatGpt3_5Turbo;
    public bool UsingByoApiKey { get; set; } = false;

    public async Task ClearHistory()
    {
        await _notifierService.CancelMessageStream();
        ChatMessages.Clear();
        await _notifierService.Update();
    }
}
