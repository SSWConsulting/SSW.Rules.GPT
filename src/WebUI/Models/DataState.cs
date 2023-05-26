using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebUI.Models;

public class DataState
{
    public string? OpenAiApiKey { get; set; }
    public List<Message> ChatMessages { get; set; } = new();
    public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
    public AvailableGptModels SelectedGptModel { get; set; } =
        (AvailableGptModels)OpenAI.GPT3.ObjectModels.Models.Model.ChatGpt3_5Turbo;
}
