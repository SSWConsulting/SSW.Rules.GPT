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
            var options = new ChatCompletionOptions { MaxOutputTokenCount = 1 };
            // GPT-5.6 rejects a custom temperature (HTTP 400), which would make key
            // validation wrongly report a valid key as invalid. See SupportsCustomTemperature.
            if (gptModel.SupportsCustomTemperature())
            {
                options.Temperature = 0.5f;
            }
            await client.CompleteChatAsync([new UserChatMessage("a")], options);

            return new ApiValidationResult(true, string.Empty);
        }
        catch (Exception ex)
        {
            return new ApiValidationResult(false, ex.Message);
        }
    }
}
