using System.Diagnostics;

namespace HealthPlanChat.WebApi.Middleware;

/// <summary>
/// Middleware for structured request logging.
/// Logs request/response metadata without sensitive content.
/// </summary>
public sealed class StructuredLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StructuredLoggingMiddleware> _logger;

    public StructuredLoggingMiddleware(RequestDelegate next, ILogger<StructuredLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Log request start (no body/query params to avoid logging user content)
        _logger.LogInformation(
            "Request started. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log request completion with timing
            _logger.LogInformation(
                "Request completed. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, DurationMs: {DurationMs}, TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);
        }
    }
}

/// <summary>
/// Extension methods for StructuredLoggingMiddleware.
/// </summary>
public static class StructuredLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds structured logging middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseStructuredLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<StructuredLoggingMiddleware>();
    }
}
