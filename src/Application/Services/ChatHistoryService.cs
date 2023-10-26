using Application.Contracts;
using Domain;
using Domain.Entities;
using Newtonsoft.Json;
using SharedClasses;

namespace Application.Services;

public class ChatHistoryService
{
    private readonly IRulesContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly SemanticKernelService _semanticKernelService;

    private string Email
    {
        get
        {
            var email = _currentUserService.GetEmail();
            if (email == null)
                throw new ArgumentException("No email found for user!");
            
            return email;
        }
    }

    public ChatHistoryService(
        IRulesContext context, 
        ICurrentUserService currentUserService,
        SemanticKernelService semanticKernelService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _semanticKernelService = semanticKernelService;
    }

    public ConversationHistoryModel? GetConversation(int id)
    {
        
        return _context.ConversationHistories
            .FirstOrDefault(s => s.Id == id && s.User == Email);
    }

    public IEnumerable<ChatHistoryDetail> GetConversations()
    {
        return _context.ConversationHistories
            .Where(s => s.User == Email)
            .OrderByDescending(s => s.Date)
            .Select(s =>
                new ChatHistoryDetail
                {
                    Id = s.Id,
                    ConversationTitle = s.ConversationTitle
                });
    }

    public async Task AddConversation(string conversation, string firstMessage)
    {
        ValidateConversation(conversation);

        var title = string.IsNullOrWhiteSpace(firstMessage) 
            ? "New conversation"
            : await GetConversationTitle(firstMessage);
        
        if (string.IsNullOrWhiteSpace(title))
            title = "New conversation";

        _context.ConversationHistories.Add(
            new ConversationHistoryModel
            {
                Date = DateTimeOffset.Now.ToUniversalTime(),
                ConversationTitle = title,
                Conversation = conversation,
                User = Email,
                SchemaVersion = 1,
            });

        await _context.SaveChangesAsync();
    }

    public async Task UpdateConversation(int id, string conversation)
    {
        ValidateConversation(conversation);

        var record = _context.ConversationHistories.FirstOrDefault(s => s.Id == id && s.User == Email);
        if (record == null)
            throw new ArgumentException("Conversation not found");
        
        record.Date = DateTimeOffset.Now.ToUniversalTime();
        record.Conversation = conversation;
        
        await _context.SaveChangesAsync();
    }

    public async Task ClearAllHistory()
    {
        _context.ConversationHistories.RemoveRange(
            _context.ConversationHistories.Where(s => s.User == Email));
        
        await _context.SaveChangesAsync();
    }

    private static void ValidateConversation(string conversation)
    {
        var deserialized = JsonConvert.DeserializeObject<ChatLinkedList>(conversation);
        if (deserialized == null)
            throw new ArgumentException("Conversation is not valid JSON");
    }
    
    private Task<string> GetConversationTitle(string firstMessage)
    {
        return _semanticKernelService.GetConversationTitle(firstMessage);
    }
}