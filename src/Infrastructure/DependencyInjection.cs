using Application.Contracts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.GPT3.Extensions;
using Pgvector.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

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
        var rateLimitPolicy = Policy.RateLimitAsync(3, TimeSpan.FromMinutes(5));

        services.AddOpenAIService(settings =>
        {
            settings.ApiKey = openAiApiKey;
        })
        .AddTransientHttpErrorPolicy(policy =>
            {
                return policy.WaitAndRetryAsync(0, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .WrapAsync(rateLimitPolicy);
            }
        );

        return services;
    }
}
