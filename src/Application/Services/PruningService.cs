using Domain;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PruningService
    {
        const int MAX_RULES_SIZE = 2000;
        const int MAX_MESSAGE_HISTORY = 8;
        const int MIN_RESPONSE_SIZE = 300;

        private readonly TokenService _tokenService;

        public PruningService(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public List<RuleDto> PruneRelevantRules(List<RuleDto> rules)
        {
            rules = rules.Where(r => r.Content != null).ToList();
            var totalTokens = _tokenService.GetTokenCount(rules);

            while (totalTokens.TokenCount > MAX_RULES_SIZE)
            {
                Console.WriteLine(
                    $"Trimmed '{rules[0].Name}' from reference rules for exceeding max allowed tokens."
                );
                rules.RemoveAt(0);
                totalTokens = _tokenService.GetTokenCount(rules);
            }

            return rules;
        }

        public TrimResult PruneMessageHistory(List<ChatMessage> messageList)
        {
            //Store the system message
            var systemMessage =
                messageList.FirstOrDefault(m => m.Role == "system")
                ?? throw new ArgumentNullException("Can't find system message.");

            if (messageList.Count + 1 > MAX_MESSAGE_HISTORY)
            {
                Console.WriteLine(
                    $"Trimmed {messageList.Count - MAX_MESSAGE_HISTORY} messages from message history for exceeding max allowed history."
                );

                messageList = messageList.TakeLast(MAX_MESSAGE_HISTORY).ToList();
                messageList.Insert(0, systemMessage);
            }

            var currentTokens = _tokenService.GetTokenCount(messageList);

            //No further trimming required
            if (currentTokens.RemainingCount >= MIN_RESPONSE_SIZE)
                return new TrimResult
                {
                    InputTooLong = false,
                    RemainingTokens = currentTokens.RemainingCount,
                    Messages = messageList
                };

            var lastMessageLength = _tokenService.GetTokenCount(
                messageList.Last(m => m.Role == "user")
            );
            var remainingTokens =
                _tokenService.GetMaxAllowedTokens()
                - MIN_RESPONSE_SIZE
                - _tokenService.GetTokenCount(systemMessage);

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
                    _tokenService.GetTokenCount(messageList[i]) is int length
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
                RemainingTokens = _tokenService.GetTokenCount(trimmedMessages).RemainingCount,
                Messages = trimmedMessages
            };
        }
    }
}
