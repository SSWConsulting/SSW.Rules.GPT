using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<EmbeddingNeighboursService>();
        services.AddScoped<MessageHandler>();
        services.AddScoped<RelevantRulesService>();
        services.AddScoped<LeaderboardService>();
        services.AddScoped<ChatHistoryService>();
        
        services.AddSingleton<ChatCompletionsService>();
        services.AddSingleton<PruningService>();
        services.AddSingleton<TokenService>();
        services.AddSingleton<OpenAiServiceFactory>();

        return services;
    }
}
