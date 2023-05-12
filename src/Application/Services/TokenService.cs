using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using SharpToken;
using Domain;

namespace Application.Services
{
    public class TokenService
    {
        const int GPT3_ALLOWED_TOKENS = 3200;
        const int GPT4_ALLOWED_TOKENS = 6400;

        public TokenResult GetTokenCount(List<ChatMessage> messageList) => GetTokenCount(messageList, Models.ChatGpt3_5Turbo);

        public TokenResult GetTokenCount(List<ChatMessage> messageList, string model)
        {
            var tokenResult = new TokenResult();
            var allowedTokens = model == Models.Gpt_4 ? GPT4_ALLOWED_TOKENS : GPT3_ALLOWED_TOKENS;
            var encodingModel = GptEncoding.GetEncodingForModel(model);

            messageList.ForEach(s => tokenResult.TokenCount += encodingModel.Encode(s.Content).Count);
            tokenResult.RemainingCount = allowedTokens - tokenResult.TokenCount;
            
            return tokenResult;
        }
    }
}
