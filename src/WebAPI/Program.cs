using System.Threading.RateLimiting;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI;
using WebAPI.Mcp;
using WebAPI.Routes;
using WebAPI.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApi(builder.Configuration);

// MCP server exposing SSW Rules to coding agents over streamable HTTP (see /mcp below).
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<SswRulesTools>();

// Per-IP rate limiting for the anonymous public endpoints (/mcp, /skills). The MCP search tool
// makes a billable OpenAI embedding call per request against a shared quota, so cap anonymous
// traffic to protect cost and to stop it starving the authenticated chat flow.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        SkillsRoutes.PublicRateLimitPolicy,
        httpContext =>
        {
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var clientKey = !string.IsNullOrWhiteSpace(forwardedFor)
                ? forwardedFor.Split(',')[0].Trim()
                : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                clientKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 30,
                    QueueLimit = 0,
                }
            );
        }
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

var baseRoute = app.MapGroup("api").RequireAuthorization();
baseRoute.MapLeaderboardRoutes();
baseRoute.MapConversationRoutes();

app.MapHealthChecks("/health");

app.MapHub<RulesHub>("/ruleshub");

// Anonymous, read-only MCP endpoint (rules are public). Mapped directly on `app` rather than the
// authorized `api` group so it doesn't require a token; rate-limited per IP against abuse.
app.MapMcp("/mcp").RequireRateLimiting(SkillsRoutes.PublicRateLimitPolicy);

// Anonymous skill distribution: GET /skills and /skills/{name} serve installable skill markdown.
app.MapSkillsRoutes();

app.Logger.LogInformation("Starting WebAPI");
await app.RunAsync();