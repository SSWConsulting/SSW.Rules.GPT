using Pgvector;
using Polly.RateLimit;

namespace Application.Contracts;

public interface IOpenAiEmbeddingService
{
    Task<Vector?> GetEmbedding(string inputString, string? apiKey);
    Func<RateLimitRejectedException, Task>? OnRateLimited { get; set; }
}
