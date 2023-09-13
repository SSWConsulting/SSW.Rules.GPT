using SharpToken;

namespace RulesEmbeddingFunction.Services;

public class TokenService
{
    private readonly GptEncoding _encoder = GptEncoding.GetEncoding("cl100k_base");

    public int GetTokenCount(string text)
        => _encoder.Encode(text).Count;
}