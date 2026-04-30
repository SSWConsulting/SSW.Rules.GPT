using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using SharedClasses;
using WebUI.Classes;

namespace WebUI.Services;

public class ApiKeyValidationService
{
    public async Task<ApiValidationResult> ValidateApiKey(string apiKey, AvailableGptModels gptModel)
    {
        try
        {
            var client = new OpenAIClient(new ApiKeyCredential(apiKey)).GetChatClient(gptModel.ToModelId());
            var options = new ChatCompletionOptions { MaxOutputTokenCount = 1, Temperature = 0.5f };
            await client.CompleteChatAsync([new UserChatMessage("a")], options);

            return new ApiValidationResult(true, string.Empty);
        }
        catch (Exception ex)
        {
            return new ApiValidationResult(false, ex.Message);
        }
    }
}
