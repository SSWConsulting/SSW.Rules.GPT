using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Interfaces;
using OpenAI.Managers;

namespace Application.Services;

public class OpenAiServiceFactory
{
    private readonly IConfiguration _config;

    private readonly ConcurrentDictionary<string, IOpenAIService> _serviceCache = new();

    public OpenAiServiceFactory(IConfiguration config)
    {
        _config = config;
    }

    public IOpenAIService Create(string? azureOpenAiResourceName)
    {
        var cacheKey = azureOpenAiResourceName ?? "";

        return _serviceCache.GetOrAdd(cacheKey, _ =>
        {
            var openAiApiKey = _config.GetValue<string>("OpenAiApiKey");
            var azureOpenAiApiKey = _config.GetValue<string>("AzureOpenAiApiKey");
            var azureOpenAiEndpoint = _config.GetValue<string>("AzureOpenAiEndpoint");
            var useAzureOpenAi = _config.GetValue<bool>("UseAzureOpenAiBool");

            if (useAzureOpenAi && azureOpenAiApiKey is not null
                               && azureOpenAiEndpoint is not null
                               && azureOpenAiResourceName is not null)
            {
                return new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = azureOpenAiApiKey,
                    ResourceName = azureOpenAiEndpoint,
                    DeploymentId = azureOpenAiResourceName,
                    ProviderType = ProviderType.Azure,
                });
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(openAiApiKey);
            return new OpenAIService(new OpenAiOptions()
            {
                ApiKey = openAiApiKey
            });
        });
    }

    public IOpenAIService GetOpenAiService(string apiKey)
    {
        return new OpenAIService(new OpenAiOptions { ApiKey = apiKey });
    }
}