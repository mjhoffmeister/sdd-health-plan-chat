using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HealthPlanChat.Web;
using HealthPlanChat.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for API calls
// ApiBaseUrl from appsettings.json; falls back to host base address for local dev
var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : configuredApiBaseUrl;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Register API client
builder.Services.AddScoped<ApiClient>();

// Register chat session service
builder.Services.AddScoped<ChatSessionService>();

// Register theme service for light/dark mode toggle
builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
