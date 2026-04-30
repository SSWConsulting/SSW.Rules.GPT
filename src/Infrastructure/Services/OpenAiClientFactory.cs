using System.ClientModel;
using System.ClientModel.Primitives;
using Application.Contracts;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using SharedClasses;

namespace Infrastructure.Services;

public class OpenAiClientFactory : IOpenAiClientFactory
{
    public const string HttpClientName = "OpenAi";
    private const string DefaultEmbeddingModel = "text-embedding-ada-002";

    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiClientFactory(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public IChatClient GetChatClient(string? apiKey, AvailableGptModels model)
    {
        var modelId = _config["GPT_Model"] ?? model.ToModelId();
        var azureDeployment = _config["Azure_Deployment_Chat"];
        var useAzure = _config.GetValue<bool>("UseAzureOpenAiBool");

        if (apiKey is not null)
        {
            return BuildOpenAiClient(apiKey).GetChatClient(modelId).AsIChatClient();
        }

        if (useAzure)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(azureDeployment);
            return BuildAzureClient().GetChatClient(azureDeployment).AsIChatClient();
        }

        var serverApiKey = _config.GetValue<string>("OpenAiApiKey");
        ArgumentException.ThrowIfNullOrWhiteSpace(serverApiKey);
        return BuildOpenAiClient(serverApiKey).GetChatClient(modelId).AsIChatClient();
    }

    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string? apiKey)
    {
        var azureDeployment = _config["Azure_Deployment_Embedding"];
        var useAzure = _config.GetValue<bool>("UseAzureOpenAiBool");

        if (apiKey is not null)
        {
            return BuildOpenAiClient(apiKey).GetEmbeddingClient(DefaultEmbeddingModel).AsIEmbeddingGenerator();
        }

        if (useAzure)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(azureDeployment);
            return BuildAzureClient().GetEmbeddingClient(azureDeployment).AsIEmbeddingGenerator();
        }

        var serverApiKey = _config.GetValue<string>("OpenAiApiKey");
        ArgumentException.ThrowIfNullOrWhiteSpace(serverApiKey);
        return BuildOpenAiClient(serverApiKey).GetEmbeddingClient(DefaultEmbeddingModel).AsIEmbeddingGenerator();
    }

    private OpenAIClient BuildOpenAiClient(string apiKey)
    {
        var options = new OpenAIClientOptions { Transport = CreateTransport() };
        return new OpenAIClient(new ApiKeyCredential(apiKey), options);
    }

    private AzureOpenAIClient BuildAzureClient()
    {
        var azureApiKey = _config.GetValue<string>("AzureOpenAiApiKey");
        var azureEndpoint = _config.GetValue<string>("AzureOpenAiEndpoint");

        ArgumentException.ThrowIfNullOrWhiteSpace(azureApiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(azureEndpoint);

        var options = new AzureOpenAIClientOptions { Transport = CreateTransport() };
        return new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(azureApiKey), options);
    }

    private HttpClientPipelineTransport CreateTransport()
        => new(_httpClientFactory.CreateClient(HttpClientName));
}
