using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using SharpToken;
using Domain;
using Domain.DTOs;

namespace Application.Services;

public class TokenService
{
    public const int Gpt3AllowedTokens = 4000;
    public const int Gpt4AllowedTokens = 8000;

    public int GetTokenCount(ChatMessage message, Models.Model model) =>
        GptEncoding.GetEncodingForModel(model.EnumToString()).Encode(message.Content).Count;

    public int GetTokenCount(RuleDto message, Models.Model model) =>
        GptEncoding.GetEncodingForModel(model.EnumToString()).Encode(message.Content).Count;

    public TokenResult GetTokenCount(List<ChatMessage> messageList, Models.Model model) =>
        GetTokenCount(messageList.Select(m => m.Content).ToList(), model);

    public TokenResult GetTokenCount(List<RuleDto> messageList, Models.Model model) =>
        GetTokenCount(messageList.Select(m => m.Content).Cast<string>().ToList(), model);

    private TokenResult GetTokenCount(List<string> messageList, Models.Model model)
    {
        var tokenResult = new TokenResult();
        var allowedTokens = model switch
        {
            Models.Model.Gpt_4 => Gpt4AllowedTokens,
            Models.Model.ChatGpt3_5Turbo => Gpt3AllowedTokens,
            _ => throw new ArgumentOutOfRangeException()
        };

        var encodingModel = GptEncoding.GetEncodingForModel(model.EnumToString());

        messageList.ForEach(s => tokenResult.TokenCount += encodingModel.Encode(s).Count);
        tokenResult.RemainingCount = allowedTokens - tokenResult.TokenCount;

        return tokenResult;
    }

    public int GetMaxAllowedTokens(Models.Model gptModel)
    {
        return gptModel switch
        {
            Models.Model.ChatGpt3_5Turbo => Gpt3AllowedTokens,
            Models.Model.Gpt_4 => Gpt4AllowedTokens,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
