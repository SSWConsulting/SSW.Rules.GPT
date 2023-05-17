using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;

namespace WebUI.Services;

public class ApiKeyValidationService
{
    //private readonly OpenAIService _openAiService;

    public async Task<bool> ValidateApiKey(string apiKey, bool useGpt4Model)
    {
        var gptModel = useGpt4Model
            ? OpenAI.GPT3.ObjectModels.Models.Gpt_4
            : OpenAI.GPT3.ObjectModels.Models.ChatGpt3_5Turbo;

        var customAiService = new OpenAIService(new OpenAiOptions() { ApiKey = apiKey });
        var completionResult = await customAiService.ChatCompletion.CreateCompletion(
            new ChatCompletionCreateRequest()
            {
                Messages = new List<ChatMessage> { new ChatMessage("user", "a") },
                MaxTokens = 1,
                Temperature = (float)0.5
            },
            gptModel
        );

        if (completionResult.Successful is false)
        {
            return false;
        }

        return true;
    }
}
