using Application.Contracts;
using Microsoft.Extensions.Configuration;
using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace Infrastructure.Services;

public class OpenAiChatCompletionsService : IOpenAiChatCompletionsService
{
    private readonly IOpenAIService _openAiService;
    private readonly IConfiguration _config;

    public OpenAiChatCompletionsService(IOpenAIService openAiService, IConfiguration config)
    {
        _openAiService = openAiService;
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
        return openAiService.ChatCompletion.CreateCompletionAsStream(
            chatCompletionCreateRequest,
            gptModelStr,
            cancellationToken
        );
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
