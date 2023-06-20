using System.Diagnostics.CodeAnalysis;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebUI.Classes;

public class ChatLinkedListItem
{
    public ChatLinkedListItem(ChatMessage message)
    {
        Message = message;
    }
    
    public ChatLinkedListItem(ChatMessage message, ChatLinkedListItem previous)
    {
        Message = message;
        Previous = previous;
    }
    
    public ChatLinkedListItem(ChatMessage message, ChatLinkedListItem? previous, ChatLinkedListItem left)
    {
        Message = message;
        Previous = previous;
        Left = left;
    }

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

    public ChatMessage Message { get; }
    public ChatLinkedListItem? Previous { get; }
    public ChatLinkedListItem? Next { get; set; }
    public ChatLinkedListItem? Left { get; set; }
    public ChatLinkedListItem? Right { get; set; }
}