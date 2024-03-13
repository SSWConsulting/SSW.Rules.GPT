using Application.Contracts;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.Services;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly IKernel _kernel;
    private readonly ISKFunction _titleFunction;

    public SemanticKernelService(IOptions<AzureOpenAiOptions> azureOpenAiOptions)
    {
        _kernel = Kernel.Builder
            .WithAzureChatCompletionService("GPT4", azureOpenAiOptions.Value.Endpoint, azureOpenAiOptions.Value.ApiKey)
            .Build();

        _titleFunction = _kernel.CreateSemanticFunction("Create a simple three word title for a conversation about '{{$input}}'. Return only plain text with no quotation marks. Use simple and short words.");
    }
    
    public async Task<string> GetConversationTitle(string question)
    {
        var result = await _titleFunction.InvokeAsync(question, _kernel);
        
        return result.ToString();
    }
}