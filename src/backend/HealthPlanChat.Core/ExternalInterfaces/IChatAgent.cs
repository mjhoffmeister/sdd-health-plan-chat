using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.UseCases.Contracts;

namespace HealthPlanChat.Core.ExternalInterfaces;

/// <summary>
/// Interface for the chat agent that generates responses.
/// </summary>
public interface IChatAgent
{
    /// <summary>
    /// Generates a response to the user's message.
    /// </summary>
    /// <param name="history">The conversation history.</param>
    /// <param name="userMessage">The current user message.</param>
    /// <param name="retrievedChunks">Chunks retrieved for grounding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated response with answer type and references.</returns>
    Task<ChatAgentResponse> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        IReadOnlyList<RetrievedChunk> retrievedChunks,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from the chat agent.
/// </summary>
/// <param name="AnswerText">The generated answer text.</param>
/// <param name="AnswerType">Whether the answer is grounded or general guidance.</param>
/// <param name="References">References to plan documents.</param>
public sealed record ChatAgentResponse(
    string AnswerText,
    AnswerType AnswerType,
    IReadOnlyList<Reference> References);
