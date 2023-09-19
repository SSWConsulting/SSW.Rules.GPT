using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.ApplicationInsights;
using WebAPI.SignalR;

namespace WebAPI;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration,
        string rulesGptCorsPolicy)
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
        
        var allowedCors = configuration.GetValue<string>("AllowedCORSOrigins");
        if (allowedCors == null)
            throw new ArgumentException("No CORS origins specified in configuration.");
        
        //Workaround for not being able to set arrays as variables in Azure
        var allowedCorsList = allowedCors.Split(",");
        
        services.AddCors(
            options =>
                options.AddPolicy(
                    name: rulesGptCorsPolicy,
                    policy =>
                        policy
                            .WithOrigins(allowedCors)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                )
        );

        return services;
    }
}
