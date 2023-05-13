using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using SharpToken;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class TokenService
    {
        const int MAX_RULES_SIZE = 2000;
        const int MAX_MESSAGE_HISTORY = 8;
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

        #region Trimming

        public List<RuleDto> PruneRelevantRules(List<RuleDto> rules)
        {
            rules = rules.Where(r => r.Content != null).ToList();
            var totalTokens = GetTokenCount(rules);

            while (totalTokens.TokenCount > MAX_RULES_SIZE)
            {
                Console.WriteLine($"Trimmed '{rules[rules.Count - 1].Name}' from reference rules for exceeding max allowed tokens.");
                rules.RemoveAt(rules.Count - 1);
                totalTokens = GetTokenCount(rules);
            }

            return rules;
        }

        public TrimResult PruneMessageHistory(List<ChatMessage> messageList)
        {
            //Store the system message
            var systemMessage = messageList.FirstOrDefault(m => m.Role == "system") ?? throw new Exception("Can't find system message.");

            if (messageList.Count + 1 > MAX_MESSAGE_HISTORY)
            {
                Console.WriteLine($"Trimmed {messageList.Count - MAX_MESSAGE_HISTORY} messages from message history for exceeding max allowed history.");

                messageList = messageList.TakeLast(MAX_MESSAGE_HISTORY).ToList();
                messageList.Insert(0, systemMessage);
            }

            var currentTokens = GetTokenCount(messageList);

            //No further trimming required
            if (currentTokens.RemainingCount >= MIN_RESPONSE_SIZE)
                return new TrimResult
                {
                    InputTooLong = false,
                    RemainingTokens = currentTokens.RemainingCount,
                    Messages = messageList
                };

            var lastMessageLength = GetTokenCount(messageList.Last(m => m.Role == "user"));
            var remainingTokens = GPT3_ALLOWED_TOKENS - MIN_RESPONSE_SIZE - GetTokenCount(systemMessage);

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

                if (GetTokenCount(messageList[i]) is int length && length <= remainingTokens)
                {
                    trimmedMessages.Insert(0, messageList[i]);
                    remainingTokens -= length;
                }

                else break;
            }

            Console.WriteLine($"Trimmed {messageList.Count - trimmedMessages.Count} messages from message history for exceeding max allowed tokens.");

            return new TrimResult
            {
                InputTooLong = false,
                RemainingTokens = GetTokenCount(trimmedMessages).RemainingCount,
                Messages = trimmedMessages
            };
        }

        #endregion
    }
}
