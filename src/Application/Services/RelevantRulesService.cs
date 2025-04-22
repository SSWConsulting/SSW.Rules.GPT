using Application.Contracts;
using Domain.DTOs;
using OpenAI.ObjectModels;
using Pgvector;
using SharedClasses;

namespace Application.Services;

public class RelevantRulesService
{
    private readonly IOpenAiEmbeddingService _openAiEmbeddingService;
    private readonly EmbeddingNeighboursService _embeddingNeighboursService;
    private readonly PruningService _pruningService;

    public RelevantRulesService(
        IOpenAiEmbeddingService openAiEmbeddingService,
        EmbeddingNeighboursService embeddingNeighboursService,
        PruningService pruningService
    )
    {
        _openAiEmbeddingService = openAiEmbeddingService;
        _embeddingNeighboursService = embeddingNeighboursService;
        _pruningService = pruningService;
    }

    public async Task<List<RuleDto>> GetRelevantRules(
        List<ChatMessage> messageList,
        string? apiKey,
        Models.Model gptModel
    )
    {
        var lastThreeUserMessagesContent = messageList
            .Where(s => s.Role == "user")
            .TakeLast(3)
            .Select(s => s.Content)
            .ToList();

        // Replicate NextJS behaviour, whether intended or not
        if (lastThreeUserMessagesContent.Count is 1)
        {
            lastThreeUserMessagesContent.Add(lastThreeUserMessagesContent.Single());
        }
        var concatenatedUserMessages = string.Join("\n", lastThreeUserMessagesContent);

        var embeddingVector = await _openAiEmbeddingService.GetEmbedding(
            concatenatedUserMessages,
            apiKey
        );

        var relevantRulesList = await _embeddingNeighboursService.CalculateNearestNeighbours(
            new List<Vector> { embeddingVector }
        );
        relevantRulesList = _pruningService.PruneRelevantRules(relevantRulesList, gptModel);

        return relevantRulesList;
    }
}
