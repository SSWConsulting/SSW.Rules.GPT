using Application;
using Infrastructure;
using WebAPI;
using WebAPI.Routes;
using WebAPI.SignalR;

var builder = WebApplication.CreateBuilder(args);

const string RulesGptCorsPolicy = nameof(RulesGptCorsPolicy);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApi(RulesGptCorsPolicy, builder.Environment);

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
