using Blazor.Analytics;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using WebUI;
using WebUI.Helpers;
using WebUI.Models;
using WebUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(
    _ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
);

builder.Services.AddTransient<CookieHandler>();

builder.Services.AddSingleton<DataState>();
builder.Services.AddSingleton<NotifierService>();
builder.Services.AddSingleton<MarkdigPipelineService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SignalRClient>();
builder.Services.AddScoped<SswRulesGptDialogService>();
builder.Services.AddScoped<ApiKeyValidationService>();

builder.Services.AddGoogleAnalytics(builder.Configuration.GetValue<string>("AnalyticsID"));
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;

    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = builder.Configuration["SigningAuthority"];
    options.ProviderOptions.ClientId = "ssw-rulesgpt";

    options.ProviderOptions.DefaultScopes.Add("email");
    options.ProviderOptions.DefaultScopes.Add("ssw-rulesgpt-api");

    options.ProviderOptions.ResponseType = "code";
});

builder.Services.AddApiAuthorization();

const string ApiClient = nameof(ApiClient);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

builder.Services
    .AddHttpClient(
        ApiClient,
        client => client.BaseAddress = new Uri(apiBaseUrl ?? throw new InvalidOperationException())
    )
    .AddHttpMessageHandler(sp => sp.GetRequiredService<CookieHandler>());

builder.Services.AddHttpClient<RulesGptClient>(ApiClient);

await builder.Build().RunAsync();