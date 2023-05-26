using Application.Contracts;
using Application.Services;
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

    public OpenAiChatCompletionsService(OpenAiServiceFactory openAiServiceFactory)
    {
        _openAiService = openAiServiceFactory.Create("GPT35Turbo");
    }

    public IAsyncEnumerable<ChatCompletionCreateResponse> CreateCompletionAsStream(
        ChatCompletionCreateRequest chatCompletionCreateRequest,
        Models.Model gptModel,
        string? apiKey,
        CancellationToken cancellationToken
    )
    {
        var openAiService = GetOpenAiService(apiKey);

        return openAiService.ChatCompletion.CreateCompletionAsStream(
            chatCompletionCreateRequest,
            gptModel.EnumToString(),
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
