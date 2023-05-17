using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ChatCompletionsService>();
        services.AddScoped<EmbeddingNeighboursService>();
        services.AddScoped<PruningService>();
        services.AddScoped<TokenService>();

        return services;
    }
}
