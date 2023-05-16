using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebUI.Models;

public class DataState
{
    public string? OpenAiApiKey { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
}
