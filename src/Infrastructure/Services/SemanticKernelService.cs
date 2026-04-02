using Application.Contracts;
using Application.Services;
using Microsoft.Extensions.Configuration;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using ChatMessage = OpenAI.ObjectModels.RequestModels.ChatMessage;

namespace Infrastructure.Services;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly OpenAiServiceFactory _openAiServiceFactory;
    private readonly string? _azureDeploymentName;
    private readonly string _gptModel;

    public SemanticKernelService(OpenAiServiceFactory openAiServiceFactory, IConfiguration configuration)
    {
        _openAiServiceFactory = openAiServiceFactory;
        _azureDeploymentName = configuration["Azure_Deployment_Chat"];
        _gptModel = configuration["GPT_Model"] ?? Models.Model.Gpt_4.EnumToString();
    }

    public async Task<string> GetConversationTitle(string question)
    {
        try
        {
            var openAiService = _openAiServiceFactory.Create(_azureDeploymentName);
            var result = await openAiService.ChatCompletion.CreateCompletion(
                new ChatCompletionCreateRequest
                {
                    Messages =
                    [
                        ChatMessage.FromSystem(
                            "Create a simple three word title for the user's conversation. Return only plain text with no quotation marks. Use simple and short words."
                        ),
                        ChatMessage.FromUser(question)
                    ],
                    MaxTokens = 12,
                    Temperature = 0.2f
                },
                _gptModel
            );

            if (!result.Successful)
            {
                return string.Empty;
            }

            return result.Choices.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
