using Microsoft.Extensions.Configuration;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;

namespace Application.Services;
public class OpenAiServiceFactory
{
    public OpenAiServiceFactory() { }
    public OpenAIService Create(IConfiguration config, string? azureOpenAiResourceName)
    {
        var openAiApiKey = config["OpenAiApiKey"];
        var azureOpenAiApiKey = config["AzureOpenAiApiKey"];
        var azureOpenAiEndpoint = config["AzureOpenAiEndpoint"];
        var useAzureOpenAi = config.GetValue<bool>("UseAzureOpenAiBool");

        OpenAIService openAiService;

        if (useAzureOpenAi && azureOpenAiApiKey is not null)
        {
            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = azureOpenAiApiKey,
                ResourceName = azureOpenAiEndpoint,
                DeploymentId = azureOpenAiResourceName,
                ProviderType = ProviderType.Azure,
            });
        }
        else
        {
            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = openAiApiKey
            });
        }

        return openAiService;
    }
}
