using Application.Contracts;
using Application.Services;
using Microsoft.Extensions.Configuration;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using Polly.RateLimit;

namespace Infrastructure.Services;

public class OpenAiChatCompletionsService : IOpenAiChatCompletionsService
{
    private readonly OpenAiServiceFactory _openAiServiceFactory;
    private readonly IConfiguration _config;
    public Func<RateLimitRejectedException, Task> OnRateLimited { get; set; }

    public OpenAiChatCompletionsService(OpenAiServiceFactory openAiServiceFactory, IConfiguration config)
    {
        _config = config;
        _openAiServiceFactory = openAiServiceFactory;
    }

    public IAsyncEnumerable<ChatCompletionCreateResponse> CreateCompletionAsStream(
        ChatCompletionCreateRequest chatCompletionCreateRequest,
        Models.Model gptModel,
        string? apiKey,
        CancellationToken cancellationToken
    )
    {
        string gptModelStr;
        
        IOpenAIService openAiService;
        
        if (apiKey is null)
        {
            openAiService = _openAiServiceFactory.Create(_config["Azure_Deployment_Chat"]);
            gptModelStr = _config["GPT_Model"] ?? gptModel.EnumToString();
        }
        else
        {
            openAiService = _openAiServiceFactory.GetOpenAiService(apiKey);
            gptModelStr = gptModel.EnumToString();
        }

        try
        {
            return openAiService.ChatCompletion.CreateCompletionAsStream(
                chatCompletionCreateRequest,
                gptModelStr, 
                true,
                cancellationToken
            );
        }
        catch (RateLimitRejectedException e)
        {
            OnRateLimited?.Invoke(e);
            return null;
        }
    }
}
