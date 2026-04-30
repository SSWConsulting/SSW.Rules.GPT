using Microsoft.Extensions.AI;
using SharedClasses;

namespace Application.Contracts;

public interface IOpenAiClientFactory
{
    IChatClient GetChatClient(string? apiKey, AvailableGptModels model);

    IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string? apiKey);
}
