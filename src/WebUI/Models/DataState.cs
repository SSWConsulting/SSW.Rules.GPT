using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebUI.Models;

public class DataState
{
    public bool Authorized { get; set; }

    public List<ChatMessage> ChatMessages { get; set; } = new();
}
