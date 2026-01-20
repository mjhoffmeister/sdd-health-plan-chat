using System.Net;
using System.Text.Json;

namespace HealthPlanChat.WebApi.Middleware;

/// <summary>
/// Middleware for handling exceptions and returning safe error responses.
/// Avoids leaking sensitive information in error messages.
/// </summary>
public sealed class SafeErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SafeErrorHandlingMiddleware> _logger;

    public SafeErrorHandlingMiddleware(RequestDelegate next, ILogger<SafeErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            // Log error category with param name (safe) without user data
            _logger.LogWarning(
                "Request validation failed. ParamName: {ParamName}, Path: {Path}, TraceId: {TraceId}",
                ex.ParamName ?? "unknown",
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, "Invalid request parameters.");
        }
        catch (KeyNotFoundException)
        {
            _logger.LogInformation(
                "Resource not found. Path: {Path}, TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorResponseAsync(context, HttpStatusCode.NotFound, "The requested resource was not found.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Request cancelled. Path: {Path}, TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorResponseAsync(context, HttpStatusCode.RequestTimeout, "The request was cancelled.");
        }
        catch (Exception ex)
        {
            // Log exception type and correlation info
            // NOTE: In development, log full exception for debugging
            _logger.LogError(
                ex, // Include full exception for stack trace
                "Unhandled exception. Type: {ExceptionType}, Path: {Path}, TraceId: {TraceId}",
                ex.GetType().Name,
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse(message, context.TraceIdentifier);
        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Standard error response format.
/// </summary>
/// <param name="Error">User-safe error message.</param>
/// <param name="TraceId">Correlation ID for support.</param>
public sealed record ErrorResponse(string Error, string TraceId);

/// <summary>
/// Extension methods for SafeErrorHandlingMiddleware.
/// </summary>
public static class SafeErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds safe error handling middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseSafeErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SafeErrorHandlingMiddleware>();
    }
}
