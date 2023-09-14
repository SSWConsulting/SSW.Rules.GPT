using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Octokit.Webhooks;
using Octokit.Webhooks.AzureFunctions;
using RulesEmbeddingFunction.Services;

var builder = new HostBuilder();

builder.ConfigureFunctionsWorkerDefaults();
builder.ConfigureAppConfiguration(config =>
{
    config.SetBasePath(Environment.CurrentDirectory);
    config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    config.AddUserSecrets<Program>();
    config.AddEnvironmentVariables();
});

builder.ConfigureServices(services =>
{
    services.AddSingleton<TokenService>();
    services.AddSingleton<EmbeddingService>();
    services.AddSingleton<DatabaseService>();
    services.AddSingleton<WebhookEventProcessor, RuleWebhookEventProcessor>();
});

builder.ConfigureGitHubWebhooks();

var host = builder.Build();

host.Run();