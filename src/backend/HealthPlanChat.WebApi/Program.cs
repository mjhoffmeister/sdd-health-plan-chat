using HealthPlanChat.Bootstrapper;
using HealthPlanChat.WebApi.Configuration;
using HealthPlanChat.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration sections
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionKey));
builder.Services.Configure<RetrievalOptions>(builder.Configuration.GetSection(RetrievalOptions.SectionKey));

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

// Add health checks
builder.Services.AddHealthChecks();

// Add application services via Bootstrapper
builder.Services.AddHealthPlanChatServices(builder.Configuration);

var app = builder.Build();

// Add structured logging first (to capture all requests)
app.UseStructuredLogging();

// Add safe error handling
app.UseSafeErrorHandling();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health check endpoint
app.MapHealthChecks("/healthz");

// TODO: Map chat endpoints (Phase 3)

app.Run();
