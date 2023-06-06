using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebUI.Classes;

public class ChatLinkedList : List<ChatLinkedListItem>
{
    public ChatLinkedListItem Add(ChatMessage message)
    {
        var newItem = new ChatLinkedListItem(message);
        Add(newItem);
        
        return newItem;
    }
    
    public ChatLinkedListItem AddAfter(ChatMessage message, ChatLinkedListItem item)
    {
        if (item != null && !Contains(item))
            throw new ArgumentException("Item is not in the list");

        var newItem = new ChatLinkedListItem(message, item);
        item.Next = newItem;
        Add(newItem);
        
        return newItem;        
    }
    
    public ChatLinkedListItem AddRight(ChatMessage newMessage, ChatLinkedListItem item)
    {
        if (!Contains(item))
            throw new ArgumentException("Item is not in the list");

        var target = item;
        
        while (target.Right is not null)
        {
            target = target.Right;
        }

        var newItem = new ChatLinkedListItem(newMessage, null, target);
        target.Right = newItem;
        Add(newItem);

        return target;
    }

    public List<ChatLinkedListItem> GetThread(ChatLinkedListItem item)
    {
        var head = item;
        
        while (item.Previous is not null)
        {
            head = item.Previous;
        }

        var thread = new List<ChatLinkedListItem>();
        var target = head;

        while (target is not null)
        {
            thread.Add(target);
            target = target.Next;
        }

        return thread;
    }
}