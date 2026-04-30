using Application.Contracts;
using Microsoft.Extensions.AI;
using Pgvector;
using Polly.RateLimit;

namespace Infrastructure.Services;

public class OpenAiEmbeddingService : IOpenAiEmbeddingService
{
    private readonly IOpenAiClientFactory _clientFactory;

    public Func<RateLimitRejectedException, Task>? OnRateLimited { get; set; }

    public OpenAiEmbeddingService(IOpenAiClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<Vector?> GetEmbedding(string inputString, string? apiKey)
    {
        var generator = _clientFactory.GetEmbeddingGenerator(apiKey);

        try
        {
            var result = await generator.GenerateAsync([inputString]);
            var embedding = result.FirstOrDefault();
            if (embedding is null)
            {
                return null;
            }

            return new Vector(embedding.Vector.ToArray());
        }
        catch (Exception ex) when (UnwrapRateLimit(ex) is { } rateLimit)
        {
            if (OnRateLimited is not null)
            {
                await OnRateLimited.Invoke(rateLimit);
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Embedding request failed: {ex.Message}");
            return null;
        }
    }

    private static RateLimitRejectedException? UnwrapRateLimit(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is RateLimitRejectedException r) return r;
            ex = ex.InnerException;
        }
        return null;
    }
}
