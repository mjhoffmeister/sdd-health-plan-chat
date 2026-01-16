namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Input boundary for the Chat use case.
/// </summary>
public interface IChatInputBoundary
{
    /// <summary>
    /// Executes the chat use case: retrieves context, invokes the agent, and stores the response.
    /// </summary>
    /// <param name="input">The chat input containing session ID and user message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat output with answer, type, and references.</returns>
    Task<ChatOutput> ExecuteAsync(ChatInput input, CancellationToken cancellationToken = default);
}
