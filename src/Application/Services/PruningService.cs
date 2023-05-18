using Domain;
using Domain.DTOs;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace Application.Services;

public class PruningService
{
    const int MaxRulesSize = 2000;
    const int MaxMessageHistory = 8;
    const int MinResponseSize = 300;

    private readonly TokenService _tokenService;

    public PruningService(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public List<RuleDto> PruneRelevantRules(List<RuleDto> rules, Models.Model gptModel)
    {
        rules = rules.Where(r => r.Content != null).ToList();
        var totalTokens = _tokenService.GetTokenCount(rules, gptModel);

        while (totalTokens.TokenCount > MaxRulesSize)
        {
            Console.WriteLine(
                $"Trimmed '{rules[0].Name}' from reference rules for exceeding max allowed tokens."
            );
            rules.RemoveAt(0);
            totalTokens = _tokenService.GetTokenCount(rules, gptModel);
        }

        return rules;
    }

    public TrimResult PruneMessageHistory(List<ChatMessage> messageList, Models.Model gptModel)
    {
        //Store the system message
        var systemMessage =
            messageList.FirstOrDefault(m => m.Role == "system")
            ?? throw new ArgumentNullException();

        if (messageList.Count + 1 > MaxMessageHistory)
        {
            Console.WriteLine(
                $"Trimmed {messageList.Count - MaxMessageHistory} messages from message history for exceeding max allowed history."
            );

            messageList = messageList.TakeLast(MaxMessageHistory).ToList();
            messageList.Insert(0, systemMessage);
        }

        var currentTokens = _tokenService.GetTokenCount(messageList, gptModel);

        //No further trimming required
        if (currentTokens.RemainingCount >= MinResponseSize)
            return new TrimResult
            {
                InputTooLong = false,
                RemainingTokens = currentTokens.RemainingCount,
                Messages = messageList
            };

        var lastMessageLength = _tokenService.GetTokenCount(
            messageList.Last(m => m.Role == "user"),
            gptModel
        );
        var remainingTokens =
            _tokenService.GetMaxAllowedTokens(gptModel)
            - MinResponseSize
            - _tokenService.GetTokenCount(systemMessage, gptModel);

        if (lastMessageLength > remainingTokens)
            return new TrimResult
            {
                InputTooLong = true,
                RemainingTokens = 0,
                Messages = messageList
            };

        var trimmedMessages = new List<ChatMessage>();

        for (int i = messageList.Count - 1; i > 0; i--)
        {
            if (messageList[i].Role == "system")
                continue;

            if (
                _tokenService.GetTokenCount(messageList[i], gptModel) is var length
                && length <= remainingTokens
            )
            {
                trimmedMessages.Insert(0, messageList[i]);
                remainingTokens -= length;
            }
            else
                break;
        }

        Console.WriteLine(
            $"Trimmed {messageList.Count - trimmedMessages.Count} messages from message history for exceeding max allowed tokens."
        );

        return new TrimResult
        {
            InputTooLong = false,
            RemainingTokens = _tokenService.GetTokenCount(trimmedMessages, gptModel).RemainingCount,
            Messages = trimmedMessages
        };
    }
}
