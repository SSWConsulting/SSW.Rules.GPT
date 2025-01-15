using Application;
using Infrastructure;
using WebAPI;
using WebAPI.Routes;
using WebAPI.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApi(builder.Configuration);

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

var baseRoute = app.MapGroup("api").RequireAuthorization();
baseRoute.MapLeaderboardRoutes();
baseRoute.MapConversationRoutes();

app.MapHealthChecks("/health");

app.MapHub<RulesHub>("/ruleshub");

app.Logger.LogInformation("Starting WebAPI");
await app.RunAsync();