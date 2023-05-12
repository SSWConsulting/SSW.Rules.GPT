using OpenAI.GPT3.Interfaces;
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

    public async IAsyncEnumerable<ChatMessage?>? RequestNewCompletionMessage(
        List<ChatMessage> messageList
    )
    {
        var tokenCount = TokenService.GetTokenCount(messageList);

        if (tokenCount.remaining <= 0)
        {
            Console.WriteLine("Too many tokens.");
            yield return new ChatMessage("assistant", "⚠️ Message too long! Please shorten your message and try again.");
            yield break;
        }

        var completionResult = _openAiService.ChatCompletion.CreateCompletionAsStream(
            new ChatCompletionCreateRequest()
            {
                Messages = messageList,
                MaxTokens = tokenCount.remaining,
                Temperature = (float)0.5
            },
            Models.ChatGpt3_5Turbo
        );

        await foreach (var completion in completionResult)
        {
            if (completion.Successful)
            {
                Console.Write(completion.Choices.FirstOrDefault()?.Message.Content);
                yield return completion.Choices.FirstOrDefault()?.Message;
            }
            else
            {
                if (completion.Error == null)
                {
                    throw new Exception("Unknown Error");
                }

                Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
            }
        }
        Console.WriteLine("Complete");
    }
}
