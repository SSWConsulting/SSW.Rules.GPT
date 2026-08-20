using Application.Contracts;
using Infrastructure.Options;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.Configure<AzureOpenAiOptions>(config.GetSection(AzureOpenAiOptions.Section));

        services.AddSingleton<IOpenAiClientFactory, OpenAiClientFactory>();
        services.AddSingleton<IOpenAiEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<ISemanticKernelService, SemanticKernelService>();
        services.AddSingleton<IRuleContentService, RuleContentService>();

        services.AddHttpClient(
            RuleContentService.HttpClientName,
            client => client.DefaultRequestHeaders.UserAgent.ParseAdd("SSW.Rules.GPT-MCP")
        );

        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<IRulesContext, RulesContext>(
            options =>
                options.UseNpgsql(connectionString, x => x.UseVector()).EnableSensitiveDataLogging()
        );

        var maxRequestsPerMinute = int.TryParse(config["MaxRequestsPerMinute"], out var parsed) ? parsed : 50;
        var rateLimitPolicy = Policy.RateLimitAsync(maxRequestsPerMinute, TimeSpan.FromSeconds(60));

        services.AddHttpClient(OpenAiClientFactory.HttpClientName)
            .AddTransientHttpErrorPolicy(builder =>
                builder.WaitAndRetryAsync(0, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .WrapAsync(rateLimitPolicy));

        return services;
    }
}
