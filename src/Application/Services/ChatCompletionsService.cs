using System.Runtime.CompilerServices;
using Application.Contracts;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace Application.Services;

public class ChatCompletionsService
{
    private readonly PruningService _pruningService;
    private readonly IOpenAiChatCompletionsService _openAiChatCompletionsService;

    public ChatCompletionsService(
        PruningService pruningService,
        IOpenAiChatCompletionsService openAiChatCompletionsService
    )
    {
        _pruningService = pruningService;
        _openAiChatCompletionsService = openAiChatCompletionsService;
    }

    public async IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey,
        Models.Model gptModel,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var trimResult = _pruningService.PruneMessageHistory(messageList);

        if (trimResult.InputTooLong)
        {
            Console.WriteLine("Too many tokens.");
            yield return new ChatMessage(
                "assistant",
                "⚠️ Message too long! Please shorten your message and try again."
            );
            yield break;
        }

        //var openAiService = GetOpenAiService(apiKey);

        var chatCompletionCreateRequest = new ChatCompletionCreateRequest
        {
            Messages = trimResult.Messages,
            MaxTokens = trimResult.RemainingTokens,
            Temperature = 0.5F
        };

        var completionResult = _openAiChatCompletionsService.CreateCompletionAsStream(
            chatCompletionCreateRequest,
            gptModel,
            apiKey: apiKey,
            cancellationToken
        );

        await foreach (var completion in completionResult.WithCancellation(cancellationToken))
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
}
