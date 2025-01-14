using Application;
using Infrastructure;
using WebAPI;
using WebAPI.Routes;
using WebAPI.SignalR;

var builder = WebApplication.CreateBuilder(args);

const string RulesGptCorsPolicy = nameof(RulesGptCorsPolicy);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApi(builder.Configuration, RulesGptCorsPolicy);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseCors(RulesGptCorsPolicy);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapLeaderboardRoutes();
app.MapConversationRoutes();
app.MapHub<RulesHub>("/ruleshub");

app.MapHealthChecks("/health");

app.Logger.LogInformation("Starting WebAPI");
await app.RunAsync();