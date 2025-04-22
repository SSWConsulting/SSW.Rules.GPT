using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Application.Services;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using Pgvector;
using Polly.RateLimit;

namespace Infrastructure.Services;

public class OpenAiEmbeddingService : IOpenAiEmbeddingService
{
    private readonly OpenAiServiceFactory _openAiServiceFactory;
    private readonly string? _azureDeploymentName;

    public Func<RateLimitRejectedException, Task>? OnRateLimited { get; set; }

    public OpenAiEmbeddingService(OpenAiServiceFactory openAiServiceFactory, IConfiguration config)
    {
        _openAiServiceFactory = openAiServiceFactory;
        _azureDeploymentName = config.GetValue<string>("Azure_Deployment_Embedding");
    }

    public async Task<Vector?> GetEmbedding(string inputString, string? apiKey)
    {
        IOpenAIService openAiService;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            openAiService = _openAiServiceFactory.Create(_azureDeploymentName);
        }
        else
        {
            openAiService = _openAiServiceFactory.GetOpenAiService(apiKey);
        }

        try
        {
            var result = await openAiService.Embeddings.CreateEmbedding(
                new EmbeddingCreateRequest { Input = inputString, Model = Models.TextEmbeddingAdaV2 }
            );

            if (result.Successful)
            {
                var embeddingResponse = result.Data.First();
                var vector = new Vector(
                    embeddingResponse.Embedding.ToArray().Select(s => (float)s).ToArray()
                );

                return vector;
            }

            if (result.Error == null)
            {
                throw new Exception("Unknown Error");
            }

            Console.WriteLine($"{result.Error.Code}: {result.Error.Message}");
            return null;
        }
        catch (RateLimitRejectedException e)
        {
            OnRateLimited?.Invoke(e);
            return null;
        }
    }
}