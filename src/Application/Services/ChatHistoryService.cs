using Application.Contracts;
using Domain;
using Domain.Entities;
using Newtonsoft.Json;
using SharedClasses;

namespace Application.Services;

public class ChatHistoryService
{
    private readonly IRulesContext _context;

    public ChatHistoryService(IRulesContext context)
    {
        _context = context;
    }

    public ConversationHistoryModel? GetConversation(int id, string email)
    {
        return _context.ConversationHistories
            .FirstOrDefault(s => s.Id == id && s.User == email);
    }

    public IEnumerable<ChatHistoryDetail> GetConversations(string user)
    {
        return _context.ConversationHistories
            .Where(s => s.User == user)
            .Select(s =>
                new ChatHistoryDetail
                {
                    Id = s.Id,
                    ConversationTitle = s.ConversationTitle
                });
    }

    public void AddConversation(string user, string conversation)
    {
        //TODO: Get title for conversation

        ValidateConversation(conversation);

        _context.ConversationHistories.Add(
            new ConversationHistoryModel
            {
                Date = DateTimeOffset.Now.ToUniversalTime(),
                ConversationTitle = "lalala",
                Conversation = conversation,
                User = user,
                SchemaVersion = 1,
            });

        _context.SaveChangesAsync();
    }

    public void UpdateConversation(int id, string email, string conversation)
    {
        ValidateConversation(conversation);

        var record = _context.ConversationHistories.FirstOrDefault(s => s.Id == id && s.User == email);
        if (record == null)
            throw new ArgumentException("Conversation not found");
        
        record.Conversation = conversation;
        
        _context.SaveChangesAsync();
    }

    public void DeleteConversation(int id, string email)
    {
        var record = _context.ConversationHistories.FirstOrDefault(s => s.Id == id && s.User == email);
        if (record == null)
            throw new ArgumentException("Conversation not found");

        _context.ConversationHistories.Remove(record);
        _context.SaveChangesAsync();
    }

    public void ClearAllHistory(string email)
    {
        _context.ConversationHistories.RemoveRange(
            _context.ConversationHistories.Where(s => s.User == email));
        
        _context.SaveChangesAsync();
    }

    private static void ValidateConversation(string conversation)
    {
        var deserialized = JsonConvert.DeserializeObject<ChatLinkedList>(conversation);
        if (deserialized == null)
            throw new ArgumentException("Conversation is not valid JSON");
    }
}