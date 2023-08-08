using Application;
using Infrastructure;
using WebAPI;
using WebAPI.Routes;
using WebAPI.SignalR;

using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

const string RulesGptCorsPolicy = nameof(RulesGptCorsPolicy);

builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication()
    .AddJwtBearer(options =>
    {
        // Configure the Authority to the expected value for
        // the authentication provider. This ensures the token
        // is appropriately validated.
        options.Authority = "https://localhost:5003";
    
        // We have to hook the OnMessageReceived event in order to
        // allow the JWT authentication handler to read the access
        // token from the query string when a WebSocket or 
        // Server-Sent Events request comes in.
    
        // Sending the access token in the query string is required when using WebSockets or ServerSentEvents
        // due to a limitation in Browser APIs. We restrict it to only calls to the
        // SignalR hub in this code.
        // See https://docs.microsoft.com/aspnet/core/signalr/security#access-token-logging
        // for more information about security considerations when using
        // the query string to transmit the access token.
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

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApi(RulesGptCorsPolicy);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseCors(RulesGptCorsPolicy);
app.UseHttpsRedirection();

app.MapAuthRoutes();
app.MapHub<RulesHub>("/ruleshub");

app.Logger.LogInformation("Starting WebAPI");
app.Run();
