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
        const int MAX_SYSTEM_PROMPT_SIZE = 2000;
        const int MIN_RESPONSE_SIZE = 300;

        const int GPT3_ALLOWED_TOKENS = 4000;
        const int GPT4_ALLOWED_TOKENS = 8000;

        #region Token Counting

        /// <summary>
        /// Get token count for a single ChatMessage.
        /// </summary>
        public int GetTokenCount(ChatMessage message)
            => GptEncoding.GetEncodingForModel(Models.ChatGpt3_5Turbo).Encode(message.Content).Count;

        /// <summary>
        /// Get token count for a single ChatMessage.
        /// </summary>
        public int GetTokenCount(ChatMessage message, string model)
            => GptEncoding.GetEncodingForModel(model).Encode(message.Content).Count;
        
        /// <summary>
        /// Get token count for a single RuleDto.
        /// </summary>
        public int GetTokenCount(RuleDto message, string model)
            => GptEncoding.GetEncodingForModel(model).Encode(message.Content).Count;

        /// <summary>
        /// Get token count for multiple ChatMessages.
        /// </summary>
        public TokenResult GetTokenCount(List<ChatMessage> messageList)
            => GetTokenCount(messageList.Select(m => m.Content).ToList(), Models.ChatGpt3_5Turbo);
        
        /// <summary>
        /// Get token count for multiple RuleDtos.
        /// </summary>
        public TokenResult GetTokenCount(List<RuleDto> messageList)
            => GetTokenCount(messageList.Select(m => m.Content).Cast<string>().ToList(), Models.ChatGpt3_5Turbo);

        private TokenResult GetTokenCount(List<string> messageList, string model)
        {
            var tokenResult = new TokenResult();
            var allowedTokens = model == Models.Gpt_4 ? GPT4_ALLOWED_TOKENS : GPT3_ALLOWED_TOKENS;
            var encodingModel = GptEncoding.GetEncodingForModel(model);

            messageList.ForEach(s => tokenResult.TokenCount += encodingModel.Encode(s).Count);
            tokenResult.RemainingCount = allowedTokens - tokenResult.TokenCount;

            return tokenResult;
        }

        #endregion

        public List<RuleDto> PruneRelevantRules(List<RuleDto> rules)
        {
            rules = rules.Where(r => r.Content != null).ToList();
            var totalTokens = GetTokenCount(rules);

            while (totalTokens.TokenCount > MAX_SYSTEM_PROMPT_SIZE)
            {
                rules.RemoveAt(rules.Count - 1);
                totalTokens = GetTokenCount(rules);
            }

            return rules;
        }
    }
}
