using Application.Contracts;
using Domain;
using Domain.Entities;
using Newtonsoft.Json;
using SharedClasses;

namespace Application.Services;

public class ChatHistoryService
{
    private readonly IRulesContext _context;
    private readonly SemanticKernelService _semanticKernelService;

    public ChatHistoryService(IRulesContext context, SemanticKernelService semanticKernelService)
    {
        _context = context;
        _semanticKernelService = semanticKernelService;
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

    public void AddConversation(string email, string conversation, string firstMessage)
    {
        ValidateConversation(conversation);

        var title = string.IsNullOrWhiteSpace(firstMessage) 
            ? "New conversation"
            : GetConversationTitle(firstMessage);
        
        if (string.IsNullOrWhiteSpace(title))
            title = "New conversation";

        _context.ConversationHistories.Add(
            new ConversationHistoryModel
            {
                Date = DateTimeOffset.Now.ToUniversalTime(),
                ConversationTitle = title,
                Conversation = conversation,
                User = email,
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
    
    private string GetConversationTitle(string firstMessage)
    {
        return _semanticKernelService.GetConversationTitle(firstMessage).Result;
    }
}