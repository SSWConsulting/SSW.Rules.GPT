using Pgvector;
using Polly.RateLimit;

namespace Application.Contracts;

public interface IOpenAiEmbeddingService
{
    public Task<List<Vector>> GetEmbeddingList(List<string> stringList, string? apiKey);
    public Task<Vector> GetEmbedding(string inputString, string? apiKey);
    public Func<RateLimitRejectedException, Task> OnRateLimited { get; set; }
}
