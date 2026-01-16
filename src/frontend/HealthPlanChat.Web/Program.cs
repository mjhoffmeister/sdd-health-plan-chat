using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HealthPlanChat.Web;
using HealthPlanChat.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for API calls
// ApiBaseUrl from appsettings.json; falls back to host base address for local dev
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Register API client
builder.Services.AddScoped<ApiClient>();

// Register chat session service
builder.Services.AddScoped<ChatSessionService>();

await builder.Build().RunAsync();
