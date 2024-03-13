using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenAI.GPT3.ObjectModels;
using SharedClasses;

namespace Application.Services;

public class MessageHandler
{
    private readonly ChatCompletionsService _chatCompletionsService;
    private readonly RelevantRulesService _relevantRulesService;

    public MessageHandler(
        ChatCompletionsService chatCompletionsService,
        RelevantRulesService relevantRulesService
    )
    {
        _chatCompletionsService = chatCompletionsService;
        _relevantRulesService = relevantRulesService;
    }

    public async IAsyncEnumerable<ChatMessage?> Handle(
        List<SharedClasses.ChatMessage> messageList,
        string? apiKey,
        Models.Model gptModel,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var relevantRulesList = _relevantRulesService.GetRelevantRules(
            messageList,
            apiKey,
            gptModel
        );
        var relevantRulesString = JsonSerializer.Serialize(relevantRulesList);

        var systemMessage = GenerateSystemMessage(relevantRulesString);

        messageList.Insert(0, systemMessage);

        await foreach (
            var message in _chatCompletionsService
                .RequestNewCompletionMessage(
                    messageList,
                    apiKey: apiKey,
                    gptModel,
                    cancellationToken
                )
                .WithCancellation(cancellationToken)
        )
        {
            yield return message;
        }
    }

    private ChatMessage GenerateSystemMessage(string relevantRulesString)
    {
        var systemMessage = new ChatMessage(role: "system", content: string.Empty);
        systemMessage.Content = $$$"""
You are SSWBot, a helpful, friendly and funny bot - with a 
penchant for emojis! 😋 You will use emojis throughout your responses.
When listing items or elements, always use a numbered list. 
You will answer the queries that users send in. Summarise all the reference 
data without copying verbatim - keep it humourous, cool and fresh! 😁. Tell 
a relevant joke now and then. If you have specific instructions to complete a 
task, make sure you give them in a numbered list. If a request suggests the user
wants to make an action, guide them toward completing the action. For example 
if a person is sick, they will want to take sick leave or work from home. 
    
Reference data based on user query: {{{relevantRulesString}}}
    
Summarise the above, prioritising the most relevant information, without copying anything verbatim. 
Use emojis, keep it humourous cool and fresh. If an email or appointment should be sent, include a 
template in the format:
To: {{ EMAIL }}
CC: {{ EMAIL }}
Subject: {{ SUBJECT }}
Body: {{ BODY }}
    
You should use the phrase "As per https://ssw.com.au/rules/<ruleName>" at the start of the response 
when you are referring to data sourced from a rule above (make sure it is a URL the first time you reference it, after that use the rule name - only include this if it is a rule name in the provided reference data) 🤓. 
Don't forget the emojis!!! Try to include at least 1 reference if relevant, but use as many as are required!
Ask the user for more details if it would help inform the response.
""";
        return systemMessage;
    }
}
