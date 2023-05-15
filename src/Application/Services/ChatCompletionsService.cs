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
    private readonly PruningService _pruningService;

    public ChatCompletionsService(IOpenAIService openAiService, PruningService pruningService)
    {
        _openAiService = openAiService;
        _pruningService = pruningService;
    }

    public async IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey
    )
    {
        var trimResult = _pruningService.PruneMessageHistory(messageList);

        if (trimResult.InputTooLong)
        {
            Console.WriteLine("Too many tokens.");
            yield return new ChatMessage("assistant", "⚠️ Message too long! Please shorten your message and try again.");
            yield break;
        }

        var openAiService = GetOpenAiService(apiKey);

        var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(

            new ChatCompletionCreateRequest()
            {
                Messages = trimResult.Messages,
                MaxTokens = trimResult.RemainingTokens,
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
