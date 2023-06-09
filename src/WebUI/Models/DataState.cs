using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebUI.Models;

public class DataState
{
    public string? OpenAiApiKey { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
    public OpenAI.GPT3.ObjectModels.Models.Model SelectedGptModel { get; set; } =
        OpenAI.GPT3.ObjectModels.Models.Model.ChatGpt3_5Turbo;
    public bool UsingByoApiKey { get; set; } = false;
}
