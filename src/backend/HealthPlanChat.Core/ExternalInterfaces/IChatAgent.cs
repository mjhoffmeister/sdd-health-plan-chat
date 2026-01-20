using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.ExternalInterfaces;

/// <summary>
/// Interface for the chat agent that generates responses.
/// The agent handles retrieval internally using configured search tools.
/// </summary>
public interface IChatAgent
{
    /// <summary>
    /// Generates a response to the user's message.
    /// The agent uses Azure AI Search tool internally to retrieve relevant plan materials.
    /// </summary>
    /// <param name="history">The conversation history.</param>
    /// <param name="userMessage">The current user message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated response with answer type and references.</returns>
    Task<ChatAgentResponse> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from the chat agent.
/// </summary>
/// <param name="AnswerText">The generated answer text.</param>
/// <param name="AnswerType">Whether the answer is grounded or general guidance.</param>
/// <param name="References">References to plan documents extracted from agent citations.</param>
public sealed record ChatAgentResponse(
    string AnswerText,
    AnswerType AnswerType,
    IReadOnlyList<Reference> References);
