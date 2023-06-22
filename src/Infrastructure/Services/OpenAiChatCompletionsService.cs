using Application.Contracts;
using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using Polly.RateLimit;

namespace Infrastructure.Services;

public class OpenAiChatCompletionsService : IOpenAiChatCompletionsService
{
    private readonly IOpenAIService _openAiService;
    
    public Func<RateLimitRejectedException, Task> OnRateLimited { get; set; }

    public OpenAiChatCompletionsService(IOpenAIService openAiService)
    {
        _openAiService = openAiService;
    }

    public IAsyncEnumerable<ChatCompletionCreateResponse> CreateCompletionAsStream(
        ChatCompletionCreateRequest chatCompletionCreateRequest,
        Models.Model gptModel,
        string? apiKey,
        CancellationToken cancellationToken
    )
    {
        var openAiService = GetOpenAiService(apiKey);

        try
        {
            return openAiService.ChatCompletion.CreateCompletionAsStream(
                chatCompletionCreateRequest,
                gptModel.EnumToString(),
                cancellationToken
            );
        }
        catch (RateLimitRejectedException e)
        {
            OnRateLimited?.Invoke(e);
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
