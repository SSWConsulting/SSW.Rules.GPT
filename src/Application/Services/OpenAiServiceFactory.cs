using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Interfaces;
using OpenAI.Managers;

namespace Application.Services;

public class OpenAiServiceFactory
{
    private readonly IConfiguration _config;

    private IOpenAIService? _gpt3Service;

    public OpenAiServiceFactory(IConfiguration config)
    {
        _config = config;
    }

    public IOpenAIService Create(string? azureOpenAiResourceName)
    {
        //TODO: Detect whether user is authenticated
        if (_gpt3Service is null)
        {
            var openAiApiKey = _config.GetValue<string>("OpenAiApiKey");
            var azureOpenAiApiKey = _config.GetValue<string>("AzureOpenAiApiKey");
            var azureOpenAiEndpoint = _config.GetValue<string>("AzureOpenAiEndpoint");
            var useAzureOpenAi = _config.GetValue<bool>("UseAzureOpenAiBool");

            OpenAIService openAiService;

            if (useAzureOpenAi && azureOpenAiApiKey is not null
                               && azureOpenAiEndpoint is not null
                               && azureOpenAiResourceName is not null)
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
                ArgumentException.ThrowIfNullOrWhiteSpace(openAiApiKey);
                openAiService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = openAiApiKey
                });
            }

            _gpt3Service = openAiService;
        }
        
        return _gpt3Service;
    }

    public IOpenAIService GetOpenAiService(string apiKey)
    {
        return new OpenAIService(new OpenAiOptions { ApiKey = apiKey });
    }
}