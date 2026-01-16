using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Core.UseCases.Contracts;
using Microsoft.Extensions.Logging;

namespace HealthPlanChat.Core.UseCases.Chat;

/// <summary>
/// Interactor that orchestrates the chat use case:
/// 1. Validates input
/// 2. Loads session history
/// 3. Stores user message
/// 4. Retrieves relevant plan materials
/// 5. Invokes the chat agent
/// 6. Stores assistant response
/// 7. Returns the output with explicit answerType and references
/// </summary>
public sealed class ChatInteractor : IChatInputBoundary
{
    private readonly IChatSessionStore _sessionStore;
    private readonly IPlanMaterialSearch _planMaterialSearch;
    private readonly IChatAgent _chatAgent;
    private readonly ILogger<ChatInteractor> _logger;
    private readonly int _topK;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatInteractor"/> class.
    /// </summary>
    /// <param name="sessionStore">The session store.</param>
    /// <param name="planMaterialSearch">The plan material search.</param>
    /// <param name="chatAgent">The chat agent.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="topK">Maximum number of chunks to retrieve. Default: 5.</param>
    public ChatInteractor(
        IChatSessionStore sessionStore,
        IPlanMaterialSearch planMaterialSearch,
        IChatAgent chatAgent,
        ILogger<ChatInteractor> logger,
        int topK = 5)
    {
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _planMaterialSearch = planMaterialSearch ?? throw new ArgumentNullException(nameof(planMaterialSearch));
        _chatAgent = chatAgent ?? throw new ArgumentNullException(nameof(chatAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _topK = topK;
    }

    /// <inheritdoc />
    public async Task<ChatOutput> ExecuteAsync(ChatInput input, CancellationToken cancellationToken = default)
    {
        // 1. Validate input
        input.Validate();

        _logger.LogInformation(
            "Processing chat request. SessionId: {SessionId}",
            input.SessionId);

        // 2. Load session to verify it exists
        var session = await _sessionStore.GetSessionAsync(input.SessionId, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("Session not found. SessionId: {SessionId}", input.SessionId);
            throw new InvalidOperationException($"Session not found: {input.SessionId}");
        }

        // 3. Get message history
        var history = await _sessionStore.GetMessagesAsync(input.SessionId, cancellationToken);

        // 4. Store user message
        var userMessage = ChatMessage.CreateUserMessage(input.SessionId, input.UserMessage);
        await _sessionStore.AppendMessageAsync(input.SessionId, userMessage, cancellationToken);

        // 5. Retrieve relevant plan materials
        var retrievedChunks = await _planMaterialSearch.SearchAsync(
            input.UserMessage,
            _topK,
            cancellationToken);

        _logger.LogInformation(
            "Retrieved {ChunkCount} chunks for grounding. SessionId: {SessionId}",
            retrievedChunks.Count,
            input.SessionId);

        // 6. Invoke chat agent
        var agentResponse = await _chatAgent.GenerateResponseAsync(
            history,
            input.UserMessage,
            retrievedChunks,
            cancellationToken);

        // 7. Store assistant response
        var assistantMessage = ChatMessage.CreateAssistantMessage(
            input.SessionId,
            agentResponse.AnswerText);
        await _sessionStore.AppendMessageAsync(input.SessionId, assistantMessage, cancellationToken);

        _logger.LogInformation(
            "Chat completed. SessionId: {SessionId}, AnswerType: {AnswerType}, ReferenceCount: {ReferenceCount}",
            input.SessionId,
            agentResponse.AnswerType,
            agentResponse.References.Count);

        // 8. Return output with explicit answerType and references
        return new ChatOutput(
            agentResponse.AnswerText,
            agentResponse.AnswerType,
            agentResponse.References);
    }
}
