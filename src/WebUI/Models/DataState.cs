using OpenAI.GPT3.ObjectModels.RequestModels;
using WebUI.Classes;

namespace WebUI.Models;

public class DataState
{
    public string? OpenAiApiKey { get; set; }
    public ChatLinkedList ChatMessages { get; } = new();
    public List<ChatLinkedListItem> CurrentMessageThread { get; set; } = new();
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public OpenAI.GPT3.ObjectModels.Models.Model SelectedGptModel { get; set; } =
        OpenAI.GPT3.ObjectModels.Models.Model.ChatGpt3_5Turbo;
    public bool UsingByoApiKey { get; set; }
    public bool IsAwaitingResponse { get; set; }
    public bool IsAwaitingResponseStream { get; set; }
    public string NewMessageString { get; set; } = "";
}
