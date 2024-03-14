using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Infrastructure.Services;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _function;

    public SemanticKernelService(IConfiguration configuration)
    {
        _kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("gpt-4", configuration.GetValue<string>("OpenAiApiKey"))
            .Build();

        _function = _kernel.CreateFunctionFromPrompt("Create a simple three word title for a conversation about '{{$input}}'. Return only plain text with no quotation marks. Use simple and short words.");
    }
    
    public async Task<string> GetConversationTitle(string question)
    {
        var result = await _kernel.InvokeAsync(_function, new KernelArguments
        {
            { "input", question }
        });
        
        return result.ToString();
    }
}