using Application.Contracts;
using Application.Services;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _config;
    public Func<RateLimitRejectedException, Task> OnRateLimited { get; set; }

    public OpenAiChatCompletionsService(OpenAiServiceFactory openAiServiceFactory, IConfiguration config)
    {
        _openAiService = openAiServiceFactory.Create("GPT35Turbo");
        _config = config;
    }

    public IAsyncEnumerable<ChatCompletionCreateResponse> CreateCompletionAsStream(
        ChatCompletionCreateRequest chatCompletionCreateRequest,
        Models.Model gptModel,
        string? apiKey,
        CancellationToken cancellationToken
    )
    {
        string gptModelStr;
        
        if (apiKey is null)
        {
            gptModelStr = _config["GPT_Model"] ?? gptModel.EnumToString();
        }
        else
        {
            gptModelStr = gptModel.EnumToString();
        }

        var openAiService = GetOpenAiService(apiKey);

        try
        {
            return openAiService.ChatCompletion.CreateCompletionAsStream(
                chatCompletionCreateRequest,
                gptModelStr,
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
