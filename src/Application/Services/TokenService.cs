using SharpToken;
using Domain;
using Domain.DTOs;
using SharedClasses;

namespace Application.Services;

public class TokenService
{
    public const int Gpt54NanoAllowedTokens = 16000;
    public const int Gpt55AllowedTokens = 32000;

    public int GetTokenCount(ChatMessage message, AvailableGptModels model)
        => GetEncoding(model).Encode(message.Content).Count;

    public int GetTokenCount(RuleDto message, AvailableGptModels model)
        => GetEncoding(model).Encode(message.Content).Count;

    public TokenResult GetTokenCount(List<ChatMessage> messageList, AvailableGptModels model)
        => GetTokenCount(messageList.Select(m => m.Content).ToList(), model);

    public TokenResult GetTokenCount(List<RuleDto> messageList, AvailableGptModels model)
        => GetTokenCount(messageList.Select(m => m.Content).Cast<string>().ToList(), model);

    private TokenResult GetTokenCount(List<string> messageList, AvailableGptModels model)
    {
        var tokenResult = new TokenResult();
        var allowedTokens = GetMaxAllowedTokens(model);
        var encoding = GetEncoding(model);

        messageList.ForEach(s => tokenResult.TokenCount += encoding.Encode(s).Count);
        tokenResult.RemainingCount = allowedTokens - tokenResult.TokenCount;

        return tokenResult;
    }

    public int GetMaxAllowedTokens(AvailableGptModels gptModel)
    {
        return gptModel switch
        {
            AvailableGptModels.Gpt54Nano => Gpt54NanoAllowedTokens,
            AvailableGptModels.Gpt55 => Gpt55AllowedTokens,
            _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
        };
    }

    private static GptEncoding GetEncoding(AvailableGptModels model)
        => GptEncoding.GetEncoding("o200k_base");
}
