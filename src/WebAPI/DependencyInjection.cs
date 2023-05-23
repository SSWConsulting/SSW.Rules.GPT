using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.ApplicationInsights;
using WebAPI.SignalR;

namespace WebAPI;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        string rulesGptCorsPolicy
    )
    {
        //services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
        services.AddSingleton<SignalRHubFilter>();

        services.AddSignalR(options => options.AddFilter<SignalRHubFilter>());
        services.AddOpenApiDocument(configure =>
        {
            configure.Title = "RulesGPT Api";
        });
        services.AddEndpointsApiExplorer();

        if (Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") == null)
        {
            services.AddLogging(options => options.AddConsole());
        }
        else
        {
            services.AddApplicationInsightsTelemetry();
            services.AddLogging(
                options =>
                    options
                        .AddApplicationInsights()
                        .AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information)
            );
        }

        // TODO: Set CORS in Bicep
        var productionCorsUrls = new string[]
        {
            // Static Web Apps
            "https://white-desert-00e3fb600.3.azurestaticapps.net",
            "https://ashy-meadow-0a2bad900.3.azurestaticapps.net",
            // Production
            "https://rulesgpt.ssw.com.au",
            "https://ssw.com.au/rulesgpt",
            "https://ssw.com.au", // TODO: Fix Host Origin Header
            // Staging
            "https://www.tfs365.com/rulesgpt",
            "https://www.tfs365.com" // TODO: Fix Host Origin Header
        };

        var developmentCorsUrls = new string[] { "https://localhost:5001" };

        services.AddCors(
            options =>
                options.AddPolicy(
                    name: rulesGptCorsPolicy,
                    policy =>
                        policy
                            .WithOrigins(
                                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                                == "Development"
                                    ? developmentCorsUrls
                                    : productionCorsUrls
                            )
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                )
        );

        return services;
    }
}
