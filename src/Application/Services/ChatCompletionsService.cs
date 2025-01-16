using System.Runtime.CompilerServices;
using Application.Contracts;
using Microsoft.Extensions.Logging;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace Application.Services;

public class ChatCompletionsService
{
    private readonly PruningService _pruningService;
    private readonly IOpenAiChatCompletionsService _openAiChatCompletionsService;
    private readonly ILogger<ChatCompletionsService> _logger;

    public ChatCompletionsService(
        PruningService pruningService,
        IOpenAiChatCompletionsService openAiChatCompletionsService,
        ILogger<ChatCompletionsService> logger)
    {
        _pruningService = pruningService;
        _openAiChatCompletionsService = openAiChatCompletionsService;
        _logger = logger;
    }

    public async IAsyncEnumerable<SharedClasses.ChatMessage?> RequestNewCompletionMessage(
        List<SharedClasses.ChatMessage> messageList,
        string? apiKey,
        Models.Model gptModel,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var trimResult = _pruningService.PruneMessageHistory(messageList, gptModel);

        if (trimResult.InputTooLong)
        {
            yield return new SharedClasses.ChatMessage(
                "assistant",
                "⚠️ Message too long! Please shorten your message and try again."
            );
            yield break;
        }
        
        var chatCompletionCreateRequest = new ChatCompletionCreateRequest
        {
            Messages = trimResult.Messages.Select(s => new ChatMessage(s.Role, s.Content, s.Name)).ToList(),
            MaxTokens = trimResult.RemainingTokens,
            Temperature = 0.5F
        };

        _logger.LogInformation(
            "User sent message '{MessageContent}' using {ModelName}.",
            messageList.Last(m => m.Role == "user").Content,
            gptModel.EnumToString()
        );

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
                var finishReason = completion.Choices.FirstOrDefault()?.FinishReason;
                if (finishReason != null && finishReason != "stop")
                {
                    _logger.LogInformation("{FinishReason}", finishReason);
                }

                var message = completion.Choices.FirstOrDefault()?.Message;
                yield return new SharedClasses.ChatMessage(message);
            }
            else
            {
                if (completion.Error == null)
                {
                    // TODO: use a specific exception
                    throw new Exception("Unknown Error");
                }

                _logger.LogError("OpenAI API request failed.");
                _logger.LogError("{ErrorCode}: {ErrorMessage}", completion.Error.Code, completion.Error.Message);

                if (gptModel == Models.Model.Gpt_4)
                {
                    yield return new SharedClasses.ChatMessage("assistant", "⚠️ *GPT-4 Request failed, reverting to GPT-3.5 Turbo.*" + Environment.NewLine);
                    await foreach (
                        var chatMessage in RequestNewCompletionMessage(messageList, apiKey,
                            Models.Model.Gpt_3_5_Turbo,
                            cancellationToken))
                    {
                        yield return chatMessage;
                    }
                } 
                
                else
                {
                    yield return new SharedClasses.ChatMessage("assistant", "❌ **OpenAI API request failed. Please try again later.**");
                }
                
            }
        }
    }
}
