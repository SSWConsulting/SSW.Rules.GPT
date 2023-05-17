using Pgvector;

namespace Application.Contracts;

public interface IOpenAiEmbeddingService
{
    public Task<List<Vector>> GetEmbeddingList(List<string> stringList, string? apiKey);
    public Task<Vector> GetEmbedding(string inputString, string? apiKey);
}
