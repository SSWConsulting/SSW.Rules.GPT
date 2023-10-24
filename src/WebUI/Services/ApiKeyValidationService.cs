using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using SharedClasses;
using WebUI.Classes;
using ChatMessage = OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage;

namespace WebUI.Services;

public class ApiKeyValidationService
{
    public async Task<ApiValidationResult> ValidateApiKey(string apiKey, AvailableGptModels gptModel)
    {
        var model = (OpenAI.GPT3.ObjectModels.Models.Model)gptModel;
        var customAiService = new OpenAIService(new OpenAiOptions { ApiKey = apiKey });
        var completionResult = await customAiService.ChatCompletion.CreateCompletion(
            new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage> { new("user", "a") },
                MaxTokens = 1,
                Temperature = 0.5f
            },
            model.EnumToString()
        );

        return new ApiValidationResult(
            completionResult.Successful,
            completionResult.Error?.Message ?? "Unknown error occurred."
        );
    }
}