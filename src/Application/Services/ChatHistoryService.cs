using Application.Contracts;
using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedClasses;

namespace Application.Services;

public class ChatHistoryService
{
    private readonly IRulesContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISemanticKernelService _semanticKernelService;

    //This is unlikely to change but if it ends up being used extract into a separate class
    private const int CurrentSchemaVersion = 1;
    
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
        ISemanticKernelService semanticKernelService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _semanticKernelService = semanticKernelService;
    }

    public async Task<ConversationHistoryModel?> GetConversation(int id)
    {
        return await _context.ConversationHistories
            .FirstOrDefaultAsync(s => s.Id == id && s.User == Email);
    }

    public async Task<List<ChatHistoryDetail>> GetConversations()
    {
        return await _context.ConversationHistories
            .Where(s => s.User == Email)
            .OrderByDescending(s => s.Date)
            .Select(s =>
                new ChatHistoryDetail
                {
                    Id = s.Id,
                    ConversationTitle = s.ConversationTitle
                }).ToListAsync();
    }

    public async Task<int> AddConversation(string conversation, string firstMessage)
    {
        ValidateConversation(conversation);

        var title = string.IsNullOrWhiteSpace(firstMessage) 
            ? "New conversation"
            : await GetConversationTitle(firstMessage);
        
        if (string.IsNullOrWhiteSpace(title))
            title = "New conversation";

        var newConversation = _context.ConversationHistories.Add(
            new ConversationHistoryModel
            {
                Date = DateTimeOffset.Now.ToUniversalTime(),
                ConversationTitle = title,
                Conversation = conversation,
                User = Email,
                SchemaVersion = CurrentSchemaVersion,
            });

        await _context.SaveChangesAsync();
        return newConversation.Entity.Id;
    }

    public async Task UpdateConversation(int id, string conversation)
    {
        ValidateConversation(conversation);

        var record = await _context.ConversationHistories.FirstOrDefaultAsync(s => s.Id == id && s.User == Email);
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

    //Checks that the conversation is a valid JSON object
    private static void ValidateConversation(string conversation)
    {
        var deserialized = JsonConvert.DeserializeObject<ChatLinkedList>(conversation);
        if (deserialized == null)
            throw new ArgumentException("Conversation is not valid JSON");
    }
    
    private async Task<string> GetConversationTitle(string firstMessage)
    {
        return await _semanticKernelService.GetConversationTitle(firstMessage);
    }
}