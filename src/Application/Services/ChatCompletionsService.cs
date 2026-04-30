using System.Runtime.CompilerServices;
using Application.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SharedClasses;
using ChatMessage = SharedClasses.ChatMessage;

namespace Application.Services;

public class ChatCompletionsService
{
    private readonly PruningService _pruningService;
    private readonly IOpenAiClientFactory _clientFactory;
    private readonly ILogger<ChatCompletionsService> _logger;

    public ChatCompletionsService(
        PruningService pruningService,
        IOpenAiClientFactory clientFactory,
        ILogger<ChatCompletionsService> logger)
    {
        _pruningService = pruningService;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatMessage?> RequestNewCompletionMessage(
        List<ChatMessage> messageList,
        string? apiKey,
        AvailableGptModels gptModel,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var trimResult = _pruningService.PruneMessageHistory(messageList, gptModel);

        if (trimResult.InputTooLong)
        {
            yield return new ChatMessage(
                "assistant",
                "⚠️ Message too long! Please shorten your message and try again."
            );
            yield break;
        }

        _logger.LogInformation(
            "User sent message '{MessageContent}' using {ModelName}.",
            messageList.Last(m => m.Role == "user").Content,
            gptModel.AvailableModelEnumToString()
        );

        var chatClient = _clientFactory.GetChatClient(apiKey, gptModel);
        var aiMessages = trimResult.Messages.Select(ToAiMessage).ToList();
        var chatOptions = new ChatOptions
        {
            Temperature = 0.5f,
            MaxOutputTokens = trimResult.RemainingTokens
        };

        var enumerator = chatClient
            .GetStreamingResponseAsync(aiMessages, chatOptions, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                ChatResponseUpdate update;
                bool failed = false;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                        break;
                    update = enumerator.Current;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OpenAI API stream failed.");
                    failed = true;
                    update = null!;
                }

                if (failed)
                {
                    await foreach (var fallback in HandleFailure(messageList, apiKey, gptModel, cancellationToken))
                        yield return fallback;
                    yield break;
                }

                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return new ChatMessage("assistant", update.Text);
                }

                if (update.FinishReason is { } reason && reason != ChatFinishReason.Stop)
                {
                    _logger.LogInformation("{FinishReason}", reason);
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private async IAsyncEnumerable<ChatMessage?> HandleFailure(
        List<ChatMessage> messageList,
        string? apiKey,
        AvailableGptModels gptModel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (gptModel == AvailableGptModels.Gpt55)
        {
            yield return new ChatMessage(
                "assistant",
                "⚠️ *GPT-5.5 Request failed, reverting to GPT-5.4 nano.*" + Environment.NewLine);

            await foreach (var chatMessage in RequestNewCompletionMessage(
                messageList, apiKey, AvailableGptModels.Gpt54Nano, cancellationToken))
            {
                yield return chatMessage;
            }
        }
        else
        {
            yield return new ChatMessage("assistant", "❌ **OpenAI API request failed. Please try again later.**");
        }
    }

    private static Microsoft.Extensions.AI.ChatMessage ToAiMessage(ChatMessage message)
    {
        var role = message.Role switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            _ => new ChatRole(message.Role)
        };
        return new Microsoft.Extensions.AI.ChatMessage(role, message.Content);
    }
}
