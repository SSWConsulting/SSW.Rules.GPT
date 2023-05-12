using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace Application.Services;

public class ChatCompletionsService
{
    private readonly IOpenAIService _openAiService;
    private readonly TokenService _tokenService;

    public ChatCompletionsService(IOpenAIService openAiService, TokenService tokenService)
    {
        _openAiService = openAiService;
        _tokenService = tokenService;
    }

    public async IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey
    )
    {

        var tokenCount = _tokenService.GetTokenCount(messageList);

        if (tokenCount.RemainingCount <= 0)
        {
            Console.WriteLine("Too many tokens.");
            yield return new ChatMessage("assistant", "⚠️ Message too long! Please shorten your message and try again.");
            yield break;
        }

        var openAiService = GetOpenAiService(apiKey);

        var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(

            new ChatCompletionCreateRequest()
            {
                Messages = messageList,
                MaxTokens = tokenCount.RemainingCount,
                Temperature = (float)0.5
            },
            Models.ChatGpt3_5Turbo
        );

        await foreach (var completion in completionResult)
        {
            if (completion.Successful)
            {
                //Console.Write(completion.Choices.FirstOrDefault()?.Message.Content);
                var finishReason = completion.Choices.FirstOrDefault()?.FinishReason;
                if (finishReason != null)
                {
                    Console.WriteLine($"Finish Reason: {finishReason}");
                }
                yield return completion.Choices.FirstOrDefault()?.Message;
            }
            else
            {
                if (completion.Error == null)
                {
                    // TODO: use a specific exception
                    throw new Exception("Unknown Error");
                }

                Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
            }
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
