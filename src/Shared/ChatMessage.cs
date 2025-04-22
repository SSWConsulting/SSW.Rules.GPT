namespace SharedClasses;

public class ChatMessage
{
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