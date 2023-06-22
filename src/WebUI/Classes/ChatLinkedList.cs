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
        
        var newItem = new ChatLinkedListItem(newMessage, item.Previous, target);

        newItem.Left = target;
        target.Right = newItem;

        Add(newItem);
        Move(item, Direction.Right);

        return newItem;
    }

    public new void Remove(ChatLinkedListItem item)
    {
        var previous = item.Previous;
        var next = item.Next;

        if (previous != null)
        {
            previous.Next = next;
        }
        
        base.Remove(item);
    }
    
    public void Move(ChatLinkedListItem item, Direction direction)
    {
        if (!Contains(item))
            throw new ArgumentException("Item is not in the list");
        
        var target = direction == Direction.Left 
            ? item.Left
            : item.Right;

        if (target == null)
        {
            return;
        }

        if (item.Previous != null)
        {
            item.Previous.Next = target;
        }
    }

    public List<ChatLinkedListItem> GetThread(ChatLinkedListItem item)
    {
        var head = GetHead(item);
        var thread = new List<ChatLinkedListItem>();
        var target = head;

        //Move from top of list to bottom
        while (target is not null)
        {
            thread.Add(target);
            target = target.Next;
        }

        return thread;
    }

    private ChatLinkedListItem GetHead(ChatLinkedListItem item)
    {
        var head = item;
        
        while (head.Previous is not null)
            head = head.Previous;

        return head;
    }
}