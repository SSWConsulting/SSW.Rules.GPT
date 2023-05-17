using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using SharpToken;
using Domain;

namespace Application.Services
{
    public class TokenService
    {
        public const int GPT3_ALLOWED_TOKENS = 4000;
        public const int GPT4_ALLOWED_TOKENS = 8000;

        public Models.Model GPTModel { get; private set; } = Models.Model.ChatGpt3_5Turbo;

        public int GetTokenCount(ChatMessage message) =>
            GptEncoding.GetEncodingForModel(Models.ChatGpt3_5Turbo).Encode(message.Content).Count;

        public int GetTokenCount(ChatMessage message, string model) =>
            GptEncoding.GetEncodingForModel(model).Encode(message.Content).Count;

        public int GetTokenCount(RuleDto message, string model) =>
            GptEncoding.GetEncodingForModel(model).Encode(message.Content).Count;

        public TokenResult GetTokenCount(List<ChatMessage> messageList) =>
            GetTokenCount(messageList.Select(m => m.Content).ToList(), Models.ChatGpt3_5Turbo);

        public TokenResult GetTokenCount(List<RuleDto> messageList) =>
            GetTokenCount(
                messageList.Select(m => m.Content).Cast<string>().ToList(),
                Models.ChatGpt3_5Turbo
            );

        private TokenResult GetTokenCount(List<string> messageList, string model)
        {
            var tokenResult = new TokenResult();
            var allowedTokens = model == Models.Gpt_4 ? GPT4_ALLOWED_TOKENS : GPT3_ALLOWED_TOKENS;
            var encodingModel = GptEncoding.GetEncodingForModel(model);

            messageList.ForEach(s => tokenResult.TokenCount += encodingModel.Encode(s).Count);
            tokenResult.RemainingCount = allowedTokens - tokenResult.TokenCount;

            return tokenResult;
        }

        public void SetModel(Models.Model model) => GPTModel = model;

        public int GetMaxAllowedTokens()
        {
            return GPTModel switch
            {
                Models.Model.ChatGpt3_5Turbo => GPT3_ALLOWED_TOKENS,
                Models.Model.Gpt_4 => GPT4_ALLOWED_TOKENS,
                _ => GPT3_ALLOWED_TOKENS,
            };
        }
    }
}
