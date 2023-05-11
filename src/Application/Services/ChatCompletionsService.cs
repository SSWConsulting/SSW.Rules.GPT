using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace Application.Services;

public class ChatCompletionsService
{
    private readonly IOpenAIService _openAiService;

    public ChatCompletionsService(IOpenAIService openAiService)
    {
        _openAiService = openAiService;
    }

    public async IAsyncEnumerable<ChatMessage?>? RequestNewCompletionMessage(
        List<ChatMessage> messageList
    )
    {
        var completionResult = _openAiService.ChatCompletion.CreateCompletionAsStream(
            new ChatCompletionCreateRequest()
            {
                Messages = messageList,
                //MaxTokens = 200,
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
