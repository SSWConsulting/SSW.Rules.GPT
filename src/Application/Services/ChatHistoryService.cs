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

        var deserialized = JsonConvert.DeserializeObject<ChatLinkedList>(conversation);
        if (deserialized == null)
            throw new ArgumentException("Conversation is not valid JSON");

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

    public void UpdateConversation(int id, string email) { }

    public void DeleteConversation() { }

    public void ClearAllHistory() { }
}