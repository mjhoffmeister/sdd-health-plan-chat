using System.Diagnostics;

namespace HealthPlanChat.WebApi.Middleware;

/// <summary>
/// Middleware for recording request timing and answer type for /api/chat endpoint.
/// Records duration and answerType without logging prompt/user content.
/// </summary>
public sealed class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only track /api/chat endpoint
        if (!context.Request.Path.StartsWithSegments("/api/chat"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Store original response body stream
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Extract answerType from response if successful
            string? answerType = null;
            if (context.Response.StatusCode == 200 && responseBody.Length > 0)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                try
                {
                    using var reader = new StreamReader(responseBody, leaveOpen: true);
                    var responseJson = await reader.ReadToEndAsync();

                    // Extract answerType without full deserialization
                    // Look for "answerType": "Grounded" or "answerType": "GeneralGuidance"
                    var answerTypeMatch = System.Text.RegularExpressions.Regex.Match(
                        responseJson,
                        @"""answerType""\s*:\s*""(\w+)""",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (answerTypeMatch.Success)
                    {
                        answerType = answerTypeMatch.Groups[1].Value;
                    }
                }
                catch
                {
                    // Ignore parsing errors - we don't want to break the response
                }
            }

            // Log timing and answer type (no user content)
            _logger.LogInformation(
                "Chat request completed. DurationMs: {DurationMs}, StatusCode: {StatusCode}, AnswerType: {AnswerType}, TraceId: {TraceId}",
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode,
                answerType ?? "Unknown",
                context.TraceIdentifier);

            // Copy response body back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }
}

/// <summary>
/// Extension methods for RequestTimingMiddleware.
/// </summary>
public static class RequestTimingMiddlewareExtensions
{
    /// <summary>
    /// Adds request timing middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestTimingMiddleware>();
    }
}
