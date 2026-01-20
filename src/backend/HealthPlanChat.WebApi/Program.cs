using HealthPlanChat.Bootstrapper;
using HealthPlanChat.Core.UseCases;
using HealthPlanChat.Core.UseCases.Chat;
using HealthPlanChat.WebApi.Configuration;
using HealthPlanChat.WebApi.Endpoints;
using HealthPlanChat.WebApi.Middleware;
using HealthPlanChat.WebApi.Presenters;

var builder = WebApplication.CreateBuilder(args);

// Add optional local configuration override (gitignored, for local dev with real Azure resources)
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

// Bind configuration sections
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionKey));
builder.Services.Configure<RetrievalOptions>(builder.Configuration.GetSection(RetrievalOptions.SectionKey));

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS for frontend (SWA) to call backend (App Service)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5000", "http://localhost:5001", "https://localhost:5001"];

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add core services via Bootstrapper (infrastructure + domain services)
builder.Services.AddHealthPlanChatCoreServices(builder.Configuration);

// Register presentation-layer services (boundaries + presenters)
// Chat use case: boundary produces IResult, interactor uses boundary for outcomes
builder.Services.AddScoped<IChatBoundary<IResult>, ChatPresenter>();
builder.Services.AddScoped<IUseCaseInteractor<ChatRequest, IResult>, ChatInteractor<IResult>>();

// Validate required configuration on startup
ValidateConfiguration(builder.Configuration);

var app = builder.Build();

// Add structured logging first (to capture all requests)
app.UseStructuredLogging();

// Add request timing for /api/chat
app.UseRequestTiming();

// Add safe error handling
app.UseSafeErrorHandling();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable CORS for frontend (SWA) to call backend (App Service)
app.UseCors();

// Health check endpoint
app.MapHealthChecks("/healthz");

// Map chat endpoints
app.MapChatEndpoints();

app.Run();

/// <summary>
/// Validates that required Azure configuration is present.
/// </summary>
static void ValidateConfiguration(IConfiguration configuration)
{
    var errors = new List<string>();

    // Validate Redis configuration (uses managed identity, needs endpoint only)
    var redisEndpoint = configuration["Redis:Endpoint"];
    if (string.IsNullOrWhiteSpace(redisEndpoint))
    {
        errors.Add("Redis:Endpoint is required");
    }

    // Validate Search configuration
    var searchEndpoint = configuration["Search:Endpoint"];
    if (string.IsNullOrWhiteSpace(searchEndpoint))
    {
        errors.Add("Search:Endpoint is required");
    }

    var searchIndexName = configuration["Search:IndexName"];
    if (string.IsNullOrWhiteSpace(searchIndexName))
    {
        errors.Add("Search:IndexName is required");
    }

    // Validate Foundry configuration
    var foundryEndpoint = configuration["Foundry:Endpoint"];
    if (string.IsNullOrWhiteSpace(foundryEndpoint))
    {
        errors.Add("Foundry:Endpoint is required");
    }

    var foundryModelDeployment = configuration["Foundry:ChatModelDeployment"];
    if (string.IsNullOrWhiteSpace(foundryModelDeployment))
    {
        errors.Add("Foundry:ChatModelDeployment is required");
    }

    // In development, log warnings instead of throwing
    if (errors.Count > 0)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var error in errors)
            {
                Console.WriteLine($"WARNING: {error}");
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Missing required configuration: {string.Join(", ", errors)}");
        }
    }
}
