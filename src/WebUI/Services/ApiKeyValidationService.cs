using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using SharedClasses;
using WebUI.Classes;
using ChatMessage = OpenAI.ObjectModels.RequestModels.ChatMessage;

namespace WebUI.Services;

public class ApiKeyValidationService
{
    public async Task<ApiValidationResult> ValidateApiKey(string apiKey, AvailableGptModels gptModel)
    {
        var model = (OpenAI.ObjectModels.Models.Model)gptModel;
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