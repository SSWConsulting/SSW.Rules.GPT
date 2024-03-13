using SharedClasses;

namespace WebUI.Models;

public class ConversationDetails
{
    public int? Id { get; set; }
    public ChatLinkedList ChatList { get; set; }
    public List<ChatLinkedListItem> CurrentThread { get; set; }

    public ConversationDetails()
    {
        ChatList = new ChatLinkedList();
        CurrentThread = new List<ChatLinkedListItem>();
    }
    
    public ConversationDetails(ChatLinkedList chatList)
    {
        ChatList = chatList;
        CurrentThread = chatList.GetThread(chatList.Last());
    }
    
    public ConversationDetails(int id, ChatLinkedList chatList)
    {
        Id = id;
        ChatList = chatList;
        CurrentThread = chatList.GetThread(chatList.Last());
    }
}