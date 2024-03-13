namespace Domain.Entities;

public class ConversationHistoryModel
{
    public int Id { get; set; } 
    public DateTimeOffset Date { get; set; }
    public string User { get; set; } = null!;
    public string ConversationTitle { get; set; } = null!;
    public int SchemaVersion { get; set; }
    public string Conversation { get; set; } = null!;
}