using SharedClasses;

namespace Domain;

public class TrimResult
{
    public bool InputTooLong { get; set; }

    public int RemainingTokens { get; set; }

    public List<ChatMessage> Messages { get; set; } = null!;
}