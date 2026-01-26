using System.Net.Http.Json;

namespace HealthPlanChat.Web.Services;

/// <summary>
/// HTTP client for communicating with the Health Plan Chat API.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _httpClient;

    // Retry settings for cold-start resilience
    private static readonly TimeSpan MaxWarmupWait = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(8);

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Sends a chat message and receives a response.
    /// Implements exponential back-off retry for transient 5xx errors during App Service cold starts.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat response.</returns>
    public async Task<ChatResponse?> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var attemptDelay = InitialRetryDelay;
        var start = DateTimeOffset.UtcNow;

        while (true)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
                }

                // Retry transient 5xx errors (cold-start scenarios)
                if (IsRetryableStatusCode(response.StatusCode) && ShouldRetry(start))
                {
                    await Task.Delay(attemptDelay, cancellationToken);
                    attemptDelay = NextDelay(attemptDelay);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return null; // Unreachable, but satisfies compiler
            }
            catch (HttpRequestException) when (!cancellationToken.IsCancellationRequested && ShouldRetry(start))
            {
                await Task.Delay(attemptDelay, cancellationToken);
                attemptDelay = NextDelay(attemptDelay);
            }
        }
    }

    private static bool ShouldRetry(DateTimeOffset start)
        => DateTimeOffset.UtcNow - start < MaxWarmupWait;

    private static TimeSpan NextDelay(TimeSpan current)
        => TimeSpan.FromMilliseconds(Math.Min(current.TotalMilliseconds * 2, MaxRetryDelay.TotalMilliseconds));

    private static bool IsRetryableStatusCode(System.Net.HttpStatusCode statusCode)
        => statusCode is System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout;

    /// <summary>
    /// Checks the health of the API.
    /// </summary>
    /// <returns>True if healthy, false otherwise.</returns>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/healthz", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Request to send a chat message.
/// </summary>
public sealed record ChatRequest(string? SessionId, string Message);

/// <summary>
/// Response containing the assistant's answer.
/// </summary>
public sealed record ChatResponse(
    string SessionId,
    AnswerType AnswerType,
    string AnswerText,
    IReadOnlyList<Reference> References);

/// <summary>
/// Indicates how the answer was derived.
/// </summary>
public enum AnswerType
{
    Grounded,
    GeneralGuidance
}

/// <summary>
/// A citation reference to a plan document.
/// </summary>
public sealed record Reference(
    string PlanDocumentId,
    string Anchor,
    string Quote);
