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
        var openAiApiKey = configuration.GetValue<string>("OpenAiApiKey") ?? throw new ArgumentNullException("OpenAiApiKey");;
        _kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("gpt-4", openAiApiKey)
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