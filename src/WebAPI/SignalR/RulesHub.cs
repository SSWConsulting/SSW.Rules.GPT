using System.Runtime.CompilerServices;
using System.Text.Json;
using Application.Services;
using Microsoft.AspNetCore.SignalR;
using Infrastructure;
using Infrastructure.Services;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Pgvector;

namespace WebAPI.SignalR;

public class RulesHub : Hub<IRulesClient>
{
    private readonly RulesContext _db;
    private readonly ChatCompletionsService _chatCompletionsService;
    private readonly OpenAiEmbeddingService _openAiEmbeddingService;
    private readonly EmbeddingNeighboursService _embeddingNeighboursService;
    private readonly PruningService _pruningService;

    public RulesHub(
        RulesContext db,
        ChatCompletionsService chatCompletionsService,
        OpenAiEmbeddingService openAiEmbeddingService,
        PruningService pruningService,
        EmbeddingNeighboursService embeddingNeighboursService
    )
    {
        _db = db;
        _chatCompletionsService = chatCompletionsService;
        _openAiEmbeddingService = openAiEmbeddingService;
        _pruningService = pruningService;
        _embeddingNeighboursService = embeddingNeighboursService;
    }

    // Server methods that a client can invoke - connection.invoke(...)
    public async Task BroadcastMessage(string user, string message)
    {
        await Clients.All.ReceiveBroadcast(user, message);
    }

    public async IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string apiKey,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var lastThreeUserMessagesContent = messageList
            .Where(s => s.Role == "user")
            .TakeLast(3)
            .Select(s => s.Content)
            .ToList();

        // Replicate NextJS behaviour, whether intended or not
        if (lastThreeUserMessagesContent.Count == 1)
        {
            lastThreeUserMessagesContent.Add(lastThreeUserMessagesContent.FirstOrDefault());
        }
        var concatenatedUserMessages = string.Join("\n", lastThreeUserMessagesContent);

        var embeddingVector = await _openAiEmbeddingService.GetEmbedding(
            concatenatedUserMessages,
            apiKey
        );
        //var embeddingVectorList = await _embeddingService.GetEmbeddingList(lastThreeUserMessagesContent);
        var relevantRulesList = await _embeddingNeighboursService.CalculateNearestNeighbours(
            new List<Vector> { embeddingVector }
        );
        relevantRulesList = _pruningService.PruneRelevantRules(relevantRulesList);

        var relevantRulesString = JsonSerializer.Serialize(relevantRulesList);

        var systemMessage = GenerateSystemMessage(relevantRulesString);

        messageList.Insert(0, systemMessage);

        await foreach (
            var message in _chatCompletionsService
                .RequestNewCompletionMessage(messageList, apiKey: apiKey, cancellationToken)
                .WithCancellation(cancellationToken)
        )
        {
            yield return message;
        }
    }

    private ChatMessage GenerateSystemMessage(string relevantRulesString)
    {
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
Ask the user for more details if it would help inform the response.
""";
        return systemMessage;
    }
}
