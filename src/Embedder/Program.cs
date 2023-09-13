using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RulesEmbeddingFunction.Services;

[assembly: UserSecretsId("b5c66657-f040-4c79-acc7-9679d8df5a12")]

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
    services.AddTransient<TokenService>();
    services.AddTransient<EmbeddingService>();
    services.AddTransient<DatabaseService>();
});

var host = builder.Build();

host.Run();