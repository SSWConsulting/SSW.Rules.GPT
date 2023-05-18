using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ChatCompletionsService>();
        services.AddScoped<EmbeddingNeighboursService>();
        services.AddSingleton<PruningService>();
        services.AddSingleton<TokenService>();

        return services;
    }
}
