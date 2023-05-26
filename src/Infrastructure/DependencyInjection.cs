using Application.Contracts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.GPT3;
using OpenAI.GPT3.Extensions;
using Pgvector.EntityFrameworkCore;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.AddSingleton<IOpenAiChatCompletionsService, OpenAiChatCompletionsService>();
        services.AddSingleton<IOpenAiEmbeddingService, OpenAiEmbeddingService>();

        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<IRulesContext, RulesContext>(
            options =>
                options.UseNpgsql(connectionString, x => x.UseVector()).EnableSensitiveDataLogging()
        );

        var openAiApiKey = config["OpenAiApiKey"];
        var azureOpenAiApiKey = config["AzureOpenAiApiKey"];
        var azureOpenAiEndpoint = config["AzureOpenAiEndpoint"];
        var useAzureOpenAi = config.GetValue<bool>("UseAzureOpenAiBool");

        if (useAzureOpenAi)
        {
            services.AddOpenAIService(settings =>
            {
                settings.ApiKey = azureOpenAiApiKey;
                settings.ResourceName = azureOpenAiEndpoint;
                settings.DeploymentId = "GPT35Turbo";
                settings.ProviderType = ProviderType.Azure;
            });
        }
        else
        {
            services.AddOpenAIService(settings =>
            {
                settings.ApiKey = openAiApiKey;
            });
        }

        return services;
    }
}
