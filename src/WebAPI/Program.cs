using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Application;
using Application.Contracts;
using Application.Services;
using Infrastructure;
using Infrastructure.Services;
using OpenAI.GPT3.Extensions;
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
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PruningService>();

builder.Services.AddScoped<ChatCompletionsService>();
builder.Services.AddScoped<EmbeddingNeighboursService>();
builder.Services.AddScoped<IOpenAiEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddScoped<IOpenAiChatCompletionsService, OpenAiChatCompletionsService>();

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

// TODO: Set CORS in Bicep
var productionCorsUrls = new string[]
{
    "https://white-desert-00e3fb600.3.azurestaticapps.net",
    "https://rulesgpt.ssw.com.au",
    "https://ssw.com.au/rulesgpt"
};

var developmentCorsUrls = new string[] { "https://localhost:5001" };

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
                            ? developmentCorsUrls
                            : productionCorsUrls
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
