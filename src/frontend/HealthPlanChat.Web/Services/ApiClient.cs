using System.Net.Http.Json;

namespace HealthPlanChat.Web.Services;

/// <summary>
/// HTTP client for communicating with the Health Plan Chat API.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Sends a chat message and receives a response.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <returns>The chat response.</returns>
    public async Task<ChatResponse?> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
    }

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
