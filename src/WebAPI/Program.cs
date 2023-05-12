using System.Numerics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Application;
using Application.Services;
using Domain;
using Domain.Entities;
using Infrastructure;
using OpenAI.GPT3.Extensions;
using Pgvector;
using WebAPI.Routes;
using WebAPI.SignalR;
using Pgvector.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ReSharper disable once RedundantAssignment
var connectionString = string.Empty;
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
else
{
    connectionString = Environment.GetEnvironmentVariable("ConnectionString");
}

builder.Services.AddDbContext<IRulesContext, RulesContext>(
    options => options.UseNpgsql(connectionString, x => x.UseVector()).EnableSensitiveDataLogging()
);

// builder.Services
//     .AddIdentityCore<User>(options =>
//     {
//         options.SignIn.RequireConfirmedAccount = false;
//         options.User.RequireUniqueEmail = false;
//         options.Password.RequireDigit = false;
//         options.Password.RequiredLength = 6;
//         options.Password.RequireNonAlphanumeric = false;
//         options.Password.RequireUppercase = false;
//         options.Password.RequireLowercase = false;
//     })
//     .AddEntityFrameworkStores<StatusContext>()
//     .AddSignInManager<SignInManager<User>>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
});

// builder.Services.ConfigureApplicationCookie(options =>
// {
//     options.Cookie.SameSite = SameSiteMode.None;
//     options.Events.OnRedirectToLogin = context =>
//     {
//         context.Response.Clear();
//         context.Response.StatusCode = 401;
//         return Task.CompletedTask;
//     };
// });
//builder.Services.AddAuthorization();
builder.Services.AddAuthentication();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddScoped<ChatCompletionsService>();
builder.Services.AddScoped<EmbeddingService>();

var openAiApiKey = string.Empty;
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    openAiApiKey = builder.Configuration["OpenAiApiKey"];
}
else
{
    openAiApiKey = Environment.GetEnvironmentVariable("OpenAiApiKey");
}

if (string.IsNullOrEmpty(openAiApiKey))
{
    throw new ArgumentNullException();
}

builder.Services.AddOpenAIService(settings =>
{
    settings.ApiKey = openAiApiKey;
});

builder.Services.AddOpenApiDocument(configure =>
{
    configure.Title = "StatusApp Api";
});

builder.Services.AddEndpointsApiExplorer();

const string StatusAppCorsPolicy = nameof(StatusAppCorsPolicy);
builder.Services.AddCors(
    options =>
        options.AddPolicy(
            name: StatusAppCorsPolicy,
            policy =>
                policy
                    .WithOrigins(
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                        == "Development"
                            ? "https://localhost:5001"
                            : "https://jolly-meadow-0638e9c00.3.azurestaticapps.net"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
        )
);

var app = builder.Build();

// CORS
app.UseCors(StatusAppCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseHttpsRedirection();

app.UseAuthentication();

// app.UseAuthorization();

using var scope = app.Services.CreateScope();
await using var db = scope.ServiceProvider.GetRequiredService<RulesContext>();

app.MapAuthRoutes();

app.MapHub<RulesHub>("/ruleshub");

app.Run();
