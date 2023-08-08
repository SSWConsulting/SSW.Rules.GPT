using Application.Contracts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.GPT3.Extensions;
using Pgvector.EntityFrameworkCore;
using Polly;

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
        var maxRequestsPerMinute = int.TryParse(config["MaxRequestsPerMinute"], out var result)
            ? result
            : 50;
        
        var rateLimitPolicy = Policy.RateLimitAsync(maxRequestsPerMinute, TimeSpan.FromSeconds(60));

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
