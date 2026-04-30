using Application.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SharedClasses;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Infrastructure.Services;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly IOpenAiClientFactory _clientFactory;
    private readonly ILogger<SemanticKernelService> _logger;

    public SemanticKernelService(
        IOpenAiClientFactory clientFactory,
        ILogger<SemanticKernelService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<string> GetConversationTitle(string question)
    {
        try
        {
            var chatClient = _clientFactory.GetChatClient(apiKey: null, AvailableGptModels.Gpt54Nano);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                    "Create a simple three word title for the user's conversation. Return only plain text with no quotation marks. Use simple and short words."),
                new(ChatRole.User, question)
            };

            var response = await chatClient.GetResponseAsync(messages, new ChatOptions
            {
                MaxOutputTokens = 12,
                Temperature = 0.2f
            });

            return response.Text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception generating conversation title");
            return string.Empty;
        }
    }
}
