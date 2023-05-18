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
        List<RuleDto> newRules = rules.Where(r => r.Similarity > 0.5 && r.Content != null).ToList();
        int totalTokens = rules.Sum(curr => _tokenService.GetTokenCount(curr, gptModel));
        while (totalTokens > MaxRulesSize)
        {
            newRules.RemoveAt(newRules.Count - 1);
            totalTokens = rules.Sum(curr => _tokenService.GetTokenCount(curr, gptModel));
        }
        return newRules;
    }

    public TrimResult PruneMessageHistory(List<ChatMessage> messageList, Models.Model gptModel)
    {
        //Store the system message
        var systemMessage =
            messageList.FirstOrDefault(m => m.Role == "system")
            ?? throw new ArgumentNullException(nameof(messageList), "No system message found.");

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

        return new TrimResult
        {
            InputTooLong = false,
            RemainingTokens = _tokenService.GetTokenCount(trimmedMessages, gptModel).RemainingCount,
            Messages = trimmedMessages
        };
    }
}
