using System.Text.Json.Serialization;

namespace SharedClasses;

public class ChatLinkedListItem
{
    public ChatLinkedListItem(ChatMessage message, AvailableGptModels gptModel)
    {
        Message = message;
        GptModel = gptModel;
    }

    public ChatLinkedListItem(ChatMessage message, ChatLinkedListItem previous, AvailableGptModels gptModel)
    {
        Message = message;
        Previous = previous;
        GptModel = gptModel;
    }

    public ChatLinkedListItem(ChatMessage message, ChatLinkedListItem? previous, ChatLinkedListItem left, AvailableGptModels gptModel)
    {
        Message = message;
        Previous = previous;
        Left = left;
        GptModel = gptModel;
    }

    [JsonIgnore]
    public int LeftCount
    {
        get
        {
            var count = 0;
            var left = Left;
            while (left is not null)
            {
                count++;
                left = left.Left;
            }

            return count;
        }
    }

    [JsonIgnore]
    public int RightCount
    {
        get
        {
            var count = 0;
            var right = Right;
            while (right is not null)
            {
                count++;
                right = right.Right;
            }

            return count;
        }
    }

    public ChatMessage Message { get; set; }
    public AvailableGptModels GptModel { get; set; }
    public ChatLinkedListItem? Previous { get; set; }
    public ChatLinkedListItem? Next { get; set; }
    public ChatLinkedListItem? Left { get; set; }
    public ChatLinkedListItem? Right { get; set; }
}