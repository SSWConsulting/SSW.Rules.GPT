using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using WebUI;
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
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<SignalRClient>();
builder.Services.AddSingleton<NotifierService>();
builder.Services.AddScoped<StatusAppDialogService>();
builder.Services.AddMudServices();

const string ApiClient = nameof(ApiClient);

var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? "https://localhost:7104"
    : "https://statusapp1.azurewebsites.net";

builder.Services
    .AddHttpClient(
        ApiClient,
        client => client.BaseAddress = new Uri(apiBaseUrl ?? throw new InvalidOperationException())
    )
    .AddHttpMessageHandler(sp => sp.GetRequiredService<CookieHandler>());

builder.Services.AddHttpClient<StatusAppClient>(ApiClient);

await builder.Build().RunAsync();
