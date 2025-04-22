using Pgvector;
using Polly.RateLimit;

namespace Application.Contracts;

public interface IOpenAiEmbeddingService
{
    public Task<Vector?> GetEmbedding(string inputString, string? apiKey);
    public Func<RateLimitRejectedException, Task>? OnRateLimited { get; set; }
}
