using Application.Contracts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // ReSharper disable once RedundantAssignment
        var connectionString = string.Empty;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }
        else
        {
            connectionString = Environment.GetEnvironmentVariable("ConnectionString");
        }

        services.AddDbContext<IRulesContext, RulesContext>(
            options =>
                options.UseNpgsql(connectionString, x => x.UseVector()).EnableSensitiveDataLogging()
        );

        var openAiApiKey = string.Empty;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            openAiApiKey = config["OpenAiApiKey"];
        }
        else
        {
            openAiApiKey = Environment.GetEnvironmentVariable("OpenAiApiKey");
        }

        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new ArgumentNullException();
        }

        services.AddOpenAIService(settings =>
        {
            settings.ApiKey = openAiApiKey;
        });

        return services;
    }
}
