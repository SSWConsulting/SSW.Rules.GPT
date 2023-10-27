namespace SharedClasses;

public class ChatMessage
{
    //Required for deserialization
    public ChatMessage() { }

    public ChatMessage(OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage message)
    {
        Role = message.Role;
        Content = message.Content;
        Name = message.Name;
    }
    
    public ChatMessage(string role, string content, string? name = null)
    {
        Role = role;
        Content = content;
        Name = name;
    }
    
    public string? Name { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }
}