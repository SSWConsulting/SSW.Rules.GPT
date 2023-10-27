using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Infrastructure.Services;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly IKernel _kernel;
    private readonly ISKFunction _titleFunction;

    public SemanticKernelService(IConfiguration configuration)
    {
        var key = configuration.GetValue<string>("AzureOpenAiApiKey");
        var endpoint = configuration.GetValue<string>("AzureOpenAiEndpoint");
        
        _kernel = Kernel.Builder
            .WithAzureChatCompletionService("GPT4", endpoint, key)
            .Build();

        _titleFunction = _kernel.CreateSemanticFunction("Create a simple three word title for a conversation about '{{$input}}'. Return only plain text with no quotation marks. Use simple and short words.");
    }
    
    public async Task<string> GetConversationTitle(string question)
    {
        var result = await _titleFunction.InvokeAsync(question, _kernel);
        
        return result.ToString();
    }
}