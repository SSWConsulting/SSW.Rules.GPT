using Application.Contracts;
using Application.Services;
using Microsoft.Extensions.Configuration;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Pgvector;
using Polly.RateLimit;

namespace Infrastructure.Services;

public class OpenAiEmbeddingService : IOpenAiEmbeddingService
{
    private readonly OpenAiServiceFactory _openAiServiceFactory;
    private readonly string azureDeploymentName;

    public Func<RateLimitRejectedException, Task> OnRateLimited { get; set; }

    public OpenAiEmbeddingService(OpenAiServiceFactory openAiServiceFactory, IConfiguration config)
    {
        _openAiServiceFactory = openAiServiceFactory;
        azureDeploymentName = config["Azure_Deployment_Embedding"];
    }

    public async Task<List<Vector>> GetEmbeddingList(List<string> stringList, string? apiKey)
    {
        var openAiService = _openAiServiceFactory.GetOpenAiService(apiKey);

        try
        {
            var result = await openAiService.Embeddings.CreateEmbedding(
                new EmbeddingCreateRequest
                {
                    InputAsList = stringList,
                    Model = Models.TextEmbeddingAdaV2
                }
            );

            if (result.Successful)
            {
                var vectorList = new List<Vector>();
                foreach (var embedding in result.Data)
                {
                    var doubleArray = embedding.Embedding.ToArray();
                    var floatArray = doubleArray.Select(s => (float)s).ToArray();
                    var vector = new Vector(floatArray);
                    vectorList.Add(vector);
                }

                return vectorList;
            }
            else
            {
                if (result.Error == null)
                {
                    throw new Exception("Unknown Error");
                }

                Console.WriteLine($"{result.Error.Code}: {result.Error.Message}");
                return null;
            }
        }
        catch (RateLimitRejectedException e)
        {
            OnRateLimited?.Invoke(e);
            return null;
        }
    }

    public async Task<Vector> GetEmbedding(string inputString, string? apiKey)
    {
        IOpenAIService openAiService;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            openAiService = _openAiServiceFactory.Create(azureDeploymentName);
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
                var embeddingResponse = result.Data.FirstOrDefault();
                var vector = new Vector(
                    embeddingResponse.Embedding.ToArray().Select(s => (float)s).ToArray()
                );

                return vector;
            }
            else
            {
                if (result.Error == null)
                {
                    throw new Exception("Unknown Error");
                }

                Console.WriteLine($"{result.Error.Code}: {result.Error.Message}");
                return null;
            }
        }
        catch (RateLimitRejectedException e)
        {
            OnRateLimited?.Invoke(e);
            return null;
        }
    }
}