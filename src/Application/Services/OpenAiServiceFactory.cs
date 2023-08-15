using Microsoft.Extensions.Configuration;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;

namespace Application.Services;
public class OpenAiServiceFactory
{
    private readonly IConfiguration _config;

    public OpenAiServiceFactory(IConfiguration config)
    {
        _config = config;
    }
    public OpenAIService Create(string? azureOpenAiResourceName)
    {
        var openAiApiKey = _config["OpenAiApiKey"];
        var azureOpenAiApiKey = _config["AzureOpenAiApiKey"];
        var azureOpenAiEndpoint = _config["AzureOpenAiEndpoint"];
        var useAzureOpenAi = _config.GetValue<bool>("UseAzureOpenAiBool");

        OpenAIService openAiService;

        if (useAzureOpenAi && azureOpenAiApiKey is not null 
            && azureOpenAiEndpoint is not null 
            && azureOpenAiResourceName is not null )
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
