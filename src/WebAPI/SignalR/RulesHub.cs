using System.Text.Json;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Domain;
using Infrastructure;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System.Text.Json.Serialization;

namespace WebAPI.SignalR;

public class RulesHub : Hub<IRulesClient>
{
    private readonly RulesContext _db;
    private readonly ChatCompletionsService _chatCompletionsService;
    private readonly EmbeddingService _embeddingService;

    public RulesHub(
        RulesContext db,
        ChatCompletionsService chatCompletionsService,
        EmbeddingService embeddingService
    )
    {
        _db = db;
        _chatCompletionsService = chatCompletionsService;
        _embeddingService = embeddingService;
    }

    // public override Task OnConnectedAsync()
    // {
    //     var userName = Context.UserIdentifier;
    //     var identityUserName = Context.User?.Identity?.Name;
    //     if (userName == null || identityUserName == null || userName != identityUserName)
    //     {
    //         Context.Abort();
    //         return base.OnConnectedAsync();
    //     }
    //
    //     var connectionPair = new Connection
    //     {
    //         UserName = userName,
    //         ConnectionId = Context.ConnectionId
    //     };
    //
    //     _db.Connections.Add(connectionPair);
    //     _db.SaveChanges();
    //
    //     return base.OnConnectedAsync();
    // }
    //
    // public override Task OnDisconnectedAsync(Exception? exception)
    // {
    //     var userName = Context.UserIdentifier;
    //     var identityUserName = Context.User?.Identity?.Name;
    //     if (userName == null || identityUserName == null || userName != identityUserName)
    //     {
    //         return base.OnDisconnectedAsync(exception);
    //     }
    //     var connectionId = Context.ConnectionId;
    //     var connectionPair = _db.Connections.First(
    //         s => s.ConnectionId == connectionId && s.UserName == userName
    //     );
    //     _db.Connections.Remove(connectionPair);
    //     _db.SaveChanges();
    //     return base.OnDisconnectedAsync(exception);
    // }

    // Server methods that a client can invoke - connection.invoke(...)
    public async Task BroadcastMessage(string user, string message)
    {
        await Clients.All.ReceiveBroadcast(user, message);
    }

    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }

    public string GetConnectionUserName()
    {
        return Context.UserIdentifier!;
    }

    public async IAsyncEnumerable<ChatMessage?>? RequestNewCompletionMessage(
        List<ChatMessage> messageList
    )
    {
        var contentList = messageList.Where(s => s.Role == "user").TakeLast(3).Select(s => s.Content).ToList();
        var embeddingVector = await _embeddingService.GetEmbedding(contentList);
        var relevantRulesList = await _embeddingService.CalculateNearestNeighbours(embeddingVector);
        var relevantRulesString = JsonSerializer.Serialize(relevantRulesList);
        
        var systemMessage = new ChatMessage(role: "system", content: string.Empty);
        systemMessage.Content = $"""
You are SSWBot, a helpful, friendly and funny bot - with a 
penchant for emojis! 😋 You will use emojis throughout your responses. 
You will answer the queries that users send in. Summarise all the reference 
data without copying verbatim - keep it humourous, cool and fresh! 😁. Tell 
a relevant joke now and then. If you have specific instructions to complete a 
task, make sure you give them in a numbered list. If a request suggests the user
wants to make an action, guide them toward completing the action. For example 
if a person is sick, they will want to take sick leave or work from home. 
    
Reference data based on user query: {relevantRulesString} 
    
Summarise the above, prioritising the most relevant information, without copying anything verbatim. 
Use emojis, keep it humourous cool and fresh. If an email or appointment should be sent, include a 
template in the format: 
To: <email> 
CC: <email> 
Subject: <subject> 
Body: <body> 
    
You should use the phrase "As per https://ssw.com.au/rules/<ruleName>" at the start of the response 
when you are referring to data sourced from a rule above (make sure it is a URL - only include this if it is a rule name in the provided reference data) 🤓. 
Don't forget the emojis!!! Try to include at least 1 reference if relevant, but use as many as are required!
Ask the user for more details if it would help inform the response.`
""";
        
        messageList.Insert(0, systemMessage);
        
        await foreach (
            var message in _chatCompletionsService.RequestNewCompletionMessage(messageList)
        )
        {
            yield return message;
        }
    }
}
