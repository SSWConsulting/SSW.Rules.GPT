using Application.Contracts;
using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Pgvector;

namespace Infrastructure.Services;

public class OpenAiEmbeddingService : IOpenAiEmbeddingService
{
    private readonly IOpenAIService _openAiService;

    public OpenAiEmbeddingService(IOpenAIService openAiService)
    {
        _openAiService = openAiService;
    }

    public async Task<List<Vector>> GetEmbeddingList(List<string> stringList, string? apiKey)
    {
        var openAiService = GetOpenAiService(apiKey);

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

    public async Task<Vector> GetEmbedding(string inputString, string? apiKey)
    {
        var openAiService = GetOpenAiService(apiKey);

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

    private IOpenAIService GetOpenAiService(string? apiKey)
    {
        if (apiKey is null)
        {
            // TODO: Check Auth once implemented
            return _openAiService;
        }

        return new OpenAIService(new OpenAiOptions() { ApiKey = apiKey });
    }
}
