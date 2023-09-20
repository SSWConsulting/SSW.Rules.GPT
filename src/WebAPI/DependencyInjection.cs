using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.ApplicationInsights;
using WebAPI.SignalR;

namespace WebAPI;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApi(this IServiceCollection services,
        string rulesGptCorsPolicy, IWebHostEnvironment env)
    {
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
            "https://ashy-meadow-0a2bad900.3.azurestaticapps.net",
            "https://white-desert-00e3fb600.3.azurestaticapps.net",
            "https://rulesgpt.ssw.com.au",
            "https://ssw.com.au/rulesgpt",
            "https://wonderful-tree-0ff091200.3.azurestaticapps.net/"
        };

        var developmentCorsUrls = new string[] { "https://localhost:5002" };

        services.AddCors(
            options =>
                options.AddPolicy(
                    name: rulesGptCorsPolicy,
                    policy =>
                        policy
                            .WithOrigins(
                                env.IsDevelopment()
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
