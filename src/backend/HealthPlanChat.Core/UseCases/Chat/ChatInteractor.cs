using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using Microsoft.Extensions.Logging;

namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Interactor that orchestrates the chat use case:
/// 1. Validates input
/// 2. Creates or loads session
/// 3. Gets message history
/// 4. Stores user message
/// 5. Invokes the chat agent (agent handles retrieval internally via Azure AI Search tool)
/// 6. Stores assistant response
/// 7. Calls boundary method with outcome
/// </summary>
/// <typeparam name="TOutput">The output type determined by the boundary.</typeparam>
public sealed class ChatInteractor<TOutput> : IUseCaseInteractor<ChatRequest, TOutput>
{
    private readonly IChatSessionStore _sessionStore;
    private readonly IChatAgent _chatAgent;
    private readonly IChatBoundary<TOutput> _boundary;
    private readonly ILogger<ChatInteractor<TOutput>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatInteractor{TOutput}"/> class.
    /// </summary>
    /// <param name="sessionStore">The session store.</param>
    /// <param name="chatAgent">The chat agent (handles retrieval internally).</param>
    /// <param name="boundary">The output boundary.</param>
    /// <param name="logger">The logger.</param>
    public ChatInteractor(
        IChatSessionStore sessionStore,
        IChatAgent chatAgent,
        IChatBoundary<TOutput> boundary,
        ILogger<ChatInteractor<TOutput>> logger)
    {
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _chatAgent = chatAgent ?? throw new ArgumentNullException(nameof(chatAgent));
        _boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TOutput> HandleAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return _boundary.ValidationFailed("Message is required.");
        }

        // 2. Create or load session
        string sessionId;
        IReadOnlyList<ChatMessage> history;

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            // Create new session
            var session = await _sessionStore.CreateSessionAsync(cancellationToken);
            sessionId = session.ChatSessionId;
            history = [];

            _logger.LogInformation(
                "Created new session. SessionId: {SessionId}",
                sessionId);
        }
        else
        {
            // Load existing session
            sessionId = request.SessionId;
            var session = await _sessionStore.GetSessionAsync(sessionId, cancellationToken);

            if (session is null)
            {
                _logger.LogWarning("Session not found. SessionId: {SessionId}", sessionId);
                return _boundary.SessionNotFound(sessionId);
            }

            history = await _sessionStore.GetMessagesAsync(sessionId, cancellationToken);

            _logger.LogInformation(
                "Loaded existing session. SessionId: {SessionId}, MessageCount: {MessageCount}",
                sessionId,
                history.Count);
        }

        // 3. Store user message
        var userMessage = ChatMessage.CreateUserMessage(sessionId, request.UserMessage);
        await _sessionStore.AppendMessageAsync(sessionId, userMessage, cancellationToken);

        // 4. Invoke chat agent (agent handles retrieval internally via Azure AI Search tool)
        var agentResponse = await _chatAgent.GenerateResponseAsync(
            history,
            request.UserMessage,
            cancellationToken);

        // 5. Store assistant response
        var assistantMessage = ChatMessage.CreateAssistantMessage(
            sessionId,
            agentResponse.AnswerText);
        await _sessionStore.AppendMessageAsync(sessionId, assistantMessage, cancellationToken);

        _logger.LogInformation(
            "Chat completed. SessionId: {SessionId}, AnswerType: {AnswerType}, ReferenceCount: {ReferenceCount}",
            sessionId,
            agentResponse.AnswerType,
            agentResponse.References.Count);

        // 6. Return output via boundary
        var response = new ChatResponse(
            sessionId,
            agentResponse.AnswerText,
            agentResponse.AnswerType,
            agentResponse.References);

        return _boundary.ChatCompleted(response);
    }
}
