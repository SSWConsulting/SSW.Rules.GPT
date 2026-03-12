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

    public SemanticKernelService(OpenAiServiceFactory openAiServiceFactory, IConfiguration configuration)
    {
        _openAiServiceFactory = openAiServiceFactory;
        _azureDeploymentName = configuration["Azure_Deployment_Chat"];
    }
    
    public async Task<string> GetConversationTitle(string question)
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
            Models.Model.Gpt_4.EnumToString()
        );

        if (!result.Successful)
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Failed to generate conversation title.");
        }

        return result.Choices.FirstOrDefault()?.Message?.Content?.Trim()
            ?? throw new InvalidOperationException("OpenAI returned an empty conversation title.");
    }
}
