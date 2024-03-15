using Application.Contracts;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.ApplicationInsights;
using WebAPI.Services;
using WebAPI.SignalR;

namespace WebAPI;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration,
        string rulesGptCorsPolicy)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
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
                            .WithOrigins(allowedCorsList)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                )
        );
        
        var signingAuthority = configuration.GetValue<string>("SigningAuthority");

        services.AddAuthentication(options =>
        {
            // Identity made Cookie authentication the default.
            // However, we want JWT Bearer Auth to be the default.
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.Authority = signingAuthority;
            options.Audience = "rulesgpt";
            options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    // If the request is for our hub...
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/ruleshub")))
                    {
                        // Read the token out of the query string
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddHealthChecks().AddDbContextCheck<RulesContext>();

        return services;
    }
}
