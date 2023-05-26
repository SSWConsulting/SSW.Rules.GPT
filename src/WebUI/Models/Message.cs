using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebUI.Models;
public class Message : ChatMessage
{
    public AvailableGptModels GptModel { get; set; } = AvailableGptModels.Gpt35Turbo;
    public Message(string role, string content, AvailableGptModels? gptModel) : base(role, content)
    {
        if (gptModel is not null)
        {
            GptModel = gptModel.Value;
        }
    }

    public ChatMessage ToChatMessage() => new (Role, Content);
}
