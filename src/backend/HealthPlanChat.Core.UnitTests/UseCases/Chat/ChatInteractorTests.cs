using FluentAssertions;
using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Core.UseCases.Chat;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthPlanChat.Core.UnitTests.UseCases.Chat;

/// <summary>
/// Unit tests for <see cref="ChatInteractor{TOutput}"/>.
/// Tests cover:
/// - Labeling: Grounded vs GeneralGuidance answer types are passed through correctly
/// - References: shape and content propagation from agent response
/// - Deterministic behavior: validation, session handling, message storage
/// </summary>
public sealed class ChatInteractorTests
{
    private readonly Mock<IChatSessionStore> _sessionStoreMock;
    private readonly Mock<IChatAgent> _chatAgentMock;
    private readonly Mock<IChatBoundary<ChatResponse>> _boundaryMock;
    private readonly Mock<ILogger<ChatInteractor<ChatResponse>>> _loggerMock;
    private readonly ChatInteractor<ChatResponse> _sut;

    private const string ValidSessionId = "session-abc123";
    private const string ValidUserMessage = "What is my deductible?";
    private const string ValidAnswerText = "Your deductible is $500 per year.";

    public ChatInteractorTests()
    {
        _sessionStoreMock = new Mock<IChatSessionStore>();
        _chatAgentMock = new Mock<IChatAgent>();
        _boundaryMock = new Mock<IChatBoundary<ChatResponse>>();
        _loggerMock = new Mock<ILogger<ChatInteractor<ChatResponse>>>();

        _sut = new ChatInteractor<ChatResponse>(
            _sessionStoreMock.Object,
            _chatAgentMock.Object,
            _boundaryMock.Object,
            _loggerMock.Object);

        // Default boundary setup: return the response directly
        _boundaryMock
            .Setup(b => b.ChatCompleted(It.IsAny<ChatResponse>()))
            .Returns((ChatResponse r) => r);

        _boundaryMock
            .Setup(b => b.ValidationFailed(It.IsAny<string>()))
            .Returns((string msg) => new ChatResponse(string.Empty, msg, AnswerType.GeneralGuidance, []));

        _boundaryMock
            .Setup(b => b.SessionNotFound(It.IsAny<string>()))
            .Returns((string id) => new ChatResponse(id, $"Session not found: {id}", AnswerType.GeneralGuidance, []));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSessionStore_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ChatInteractor<ChatResponse>(
            null!,
            _chatAgentMock.Object,
            _boundaryMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionStore");
    }

    [Fact]
    public void Constructor_WithNullChatAgent_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ChatInteractor<ChatResponse>(
            _sessionStoreMock.Object,
            null!,
            _boundaryMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("chatAgent");
    }

    [Fact]
    public void Constructor_WithNullBoundary_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ChatInteractor<ChatResponse>(
            _sessionStoreMock.Object,
            _chatAgentMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("boundary");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ChatInteractor<ChatResponse>(
            _sessionStoreMock.Object,
            _chatAgentMock.Object,
            _boundaryMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task HandleAsync_WithNullMessage_ReturnsValidationFailed()
    {
        // Arrange
        var request = new ChatRequest(ValidSessionId, null!);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        _boundaryMock.Verify(b => b.ValidationFailed("Message is required."), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyMessage_ReturnsValidationFailed()
    {
        // Arrange
        var request = new ChatRequest(ValidSessionId, string.Empty);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        _boundaryMock.Verify(b => b.ValidationFailed("Message is required."), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceMessage_ReturnsValidationFailed()
    {
        // Arrange
        var request = new ChatRequest(ValidSessionId, "   ");

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        _boundaryMock.Verify(b => b.ValidationFailed("Message is required."), Times.Once);
    }

    #endregion

    #region Session Handling Tests

    [Fact]
    public async Task HandleAsync_WithNullSessionId_CreatesNewSession()
    {
        // Arrange
        var newSession = ChatSession.Create(TimeSpan.FromHours(1));
        SetupNewSessionScenario(newSession);

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        await _sut.HandleAsync(request);

        // Assert
        _sessionStoreMock.Verify(s => s.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sessionStoreMock.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithEmptySessionId_CreatesNewSession()
    {
        // Arrange
        var newSession = ChatSession.Create(TimeSpan.FromHours(1));
        SetupNewSessionScenario(newSession);

        var request = new ChatRequest(string.Empty, ValidUserMessage);

        // Act
        await _sut.HandleAsync(request);

        // Assert
        _sessionStoreMock.Verify(s => s.CreateSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExistingSessionId_LoadsSession()
    {
        // Arrange
        var existingSession = ChatSession.Create(ValidSessionId, TimeSpan.FromHours(1));
        SetupExistingSessionScenario(existingSession);

        var request = new ChatRequest(ValidSessionId, ValidUserMessage);

        // Act
        await _sut.HandleAsync(request);

        // Assert
        _sessionStoreMock.Verify(s => s.GetSessionAsync(ValidSessionId, It.IsAny<CancellationToken>()), Times.Once);
        _sessionStoreMock.Verify(s => s.GetMessagesAsync(ValidSessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentSessionId_ReturnsSessionNotFound()
    {
        // Arrange
        _sessionStoreMock
            .Setup(s => s.GetSessionAsync(ValidSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var request = new ChatRequest(ValidSessionId, ValidUserMessage);

        // Act
        await _sut.HandleAsync(request);

        // Assert
        _boundaryMock.Verify(b => b.SessionNotFound(ValidSessionId), Times.Once);
    }

    #endregion

    #region Labeling Tests (Grounded vs GeneralGuidance)

    [Fact]
    public async Task HandleAsync_WhenAgentReturnsGrounded_ResponseHasGroundedAnswerType()
    {
        // Arrange
        var references = new List<Reference>
        {
            new("plan-001", "Section 3.2", "Your annual deductible is $500.")
        };
        var agentResponse = new ChatAgentResponse(ValidAnswerText, AnswerType.Grounded, references);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.AnswerType.Should().Be(AnswerType.Grounded);
    }

    [Fact]
    public async Task HandleAsync_WhenAgentReturnsGeneralGuidance_ResponseHasGeneralGuidanceAnswerType()
    {
        // Arrange
        var agentResponse = new ChatAgentResponse(
            "I don't have specific information about that in the plan materials.",
            AnswerType.GeneralGuidance,
            []);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, "What is the best plan for my diabetes?");

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.AnswerType.Should().Be(AnswerType.GeneralGuidance);
    }

    [Fact]
    public async Task HandleAsync_AnswerTypePropagatesFromAgentUnchanged()
    {
        // Arrange - Verify both answer types propagate correctly
        var groundedResponse = new ChatAgentResponse("Grounded answer", AnswerType.Grounded, [new("doc-1", "Section 1", "quote")]);
        var guidanceResponse = new ChatAgentResponse("General guidance", AnswerType.GeneralGuidance, []);

        var session1 = ChatSession.Create(TimeSpan.FromHours(1));
        var session2 = ChatSession.Create(TimeSpan.FromHours(1));

        // First call - Grounded
        SetupNewSessionWithAgentResponse(groundedResponse, session1);
        var result1 = await _sut.HandleAsync(new ChatRequest(null, "Grounded question"));

        // Reset and setup for second call - GeneralGuidance
        SetupNewSessionWithAgentResponse(guidanceResponse, session2);
        var result2 = await _sut.HandleAsync(new ChatRequest(null, "Guidance question"));

        // Assert
        result1.AnswerType.Should().Be(AnswerType.Grounded);
        result2.AnswerType.Should().Be(AnswerType.GeneralGuidance);
    }

    #endregion

    #region References Shape Tests

    [Fact]
    public async Task HandleAsync_WhenAgentReturnsReferences_ResponseContainsReferences()
    {
        // Arrange
        var references = new List<Reference>
        {
            new("plan-ppo-silver", "Benefits Summary", "Primary care visits: $30 copay"),
            new("plan-ppo-silver", "Cost Sharing", "After deductible is met")
        };
        var agentResponse = new ChatAgentResponse(ValidAnswerText, AnswerType.Grounded, references);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.References.Should().HaveCount(2);
        result.References.Should().BeEquivalentTo(references);
    }

    [Fact]
    public async Task HandleAsync_WhenAgentReturnsEmptyReferences_ResponseHasEmptyReferences()
    {
        // Arrange
        var agentResponse = new ChatAgentResponse(
            "General guidance answer",
            AnswerType.GeneralGuidance,
            []);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, "Out of scope question");

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.References.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ReferencesContainCorrectProperties()
    {
        // Arrange
        var references = new List<Reference>
        {
            new("contoso-hmo-gold-2026", "Deductibles", "Individual deductible: $1,000")
        };
        var agentResponse = new ChatAgentResponse(ValidAnswerText, AnswerType.Grounded, references);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        var reference = result.References.First();
        reference.PlanDocumentId.Should().Be("contoso-hmo-gold-2026");
        reference.Anchor.Should().Be("Deductibles");
        reference.Quote.Should().Be("Individual deductible: $1,000");
    }

    [Fact]
    public async Task HandleAsync_MultipleReferences_AllPropagated()
    {
        // Arrange
        var references = new List<Reference>
        {
            new("doc-1", "Section A", "Quote from A"),
            new("doc-1", "Section B", "Quote from B"),
            new("doc-2", "Overview", "Quote from doc 2"),
            new("doc-3", "Summary", "Quote from doc 3")
        };
        var agentResponse = new ChatAgentResponse("Multi-reference answer", AnswerType.Grounded, references);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, "Multi-section question");

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.References.Should().HaveCount(4);
        result.References.Select(r => r.PlanDocumentId).Should().Contain(["doc-1", "doc-2", "doc-3"]);
    }

    #endregion

    #region Deterministic Behavior Tests

    [Fact]
    public async Task HandleAsync_StoresUserMessageBeforeCallingAgent()
    {
        // Arrange
        var session = ChatSession.Create(TimeSpan.FromHours(1));
        var callOrder = new List<string>();

        _sessionStoreMock
            .Setup(s => s.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _sessionStoreMock
            .Setup(s => s.AppendMessageAsync(It.IsAny<string>(), It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Callback<string, ChatMessage, CancellationToken>((_, msg, _) =>
            {
                callOrder.Add($"AppendMessage:{msg.Role}");
            })
            .Returns(Task.CompletedTask);

        _chatAgentMock
            .Setup(a => a.GenerateResponseAsync(
                It.IsAny<IReadOnlyList<ChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GenerateResponse"))
            .ReturnsAsync(new ChatAgentResponse("Answer", AnswerType.Grounded, []));

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        await _sut.HandleAsync(request);

        // Assert - User message stored before agent call, assistant message stored after
        callOrder.Should().ContainInOrder("AppendMessage:User", "GenerateResponse", "AppendMessage:Assistant");
    }

    [Fact]
    public async Task HandleAsync_StoresAssistantMessageAfterAgentResponse()
    {
        // Arrange
        var session = ChatSession.Create(TimeSpan.FromHours(1));
        ChatMessage? storedAssistantMessage = null;

        _sessionStoreMock
            .Setup(s => s.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _sessionStoreMock
            .Setup(s => s.AppendMessageAsync(
                It.IsAny<string>(),
                It.Is<ChatMessage>(m => m.Role == Role.Assistant),
                It.IsAny<CancellationToken>()))
            .Callback<string, ChatMessage, CancellationToken>((_, msg, _) => storedAssistantMessage = msg)
            .Returns(Task.CompletedTask);

        _sessionStoreMock
            .Setup(s => s.AppendMessageAsync(
                It.IsAny<string>(),
                It.Is<ChatMessage>(m => m.Role == Role.User),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _chatAgentMock
            .Setup(a => a.GenerateResponseAsync(
                It.IsAny<IReadOnlyList<ChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatAgentResponse(ValidAnswerText, AnswerType.Grounded, []));

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        await _sut.HandleAsync(request);

        // Assert
        storedAssistantMessage.Should().NotBeNull();
        storedAssistantMessage!.Role.Should().Be(Role.Assistant);
        storedAssistantMessage.Text.Should().Be(ValidAnswerText);
    }

    [Fact]
    public async Task HandleAsync_PassesMessageHistoryToAgent()
    {
        // Arrange
        var existingSession = ChatSession.Create(ValidSessionId, TimeSpan.FromHours(1));
        var existingMessages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(ValidSessionId, "Previous question"),
            ChatMessage.CreateAssistantMessage(ValidSessionId, "Previous answer")
        };

        IReadOnlyList<ChatMessage>? passedHistory = null;

        _sessionStoreMock
            .Setup(s => s.GetSessionAsync(ValidSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        _sessionStoreMock
            .Setup(s => s.GetMessagesAsync(ValidSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMessages);

        _sessionStoreMock
            .Setup(s => s.AppendMessageAsync(It.IsAny<string>(), It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _chatAgentMock
            .Setup(a => a.GenerateResponseAsync(
                It.IsAny<IReadOnlyList<ChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<ChatMessage>, string, CancellationToken>((h, _, _) => passedHistory = h)
            .ReturnsAsync(new ChatAgentResponse("Follow-up answer", AnswerType.Grounded, []));

        var request = new ChatRequest(ValidSessionId, "Follow-up question");

        // Act
        await _sut.HandleAsync(request);

        // Assert
        passedHistory.Should().NotBeNull();
        passedHistory.Should().HaveCount(2);
        passedHistory![0].Text.Should().Be("Previous question");
        passedHistory[1].Text.Should().Be("Previous answer");
    }

    [Fact]
    public async Task HandleAsync_ReturnsNewSessionIdWhenCreated()
    {
        // Arrange
        var newSession = ChatSession.Create(TimeSpan.FromHours(1));
        SetupNewSessionWithAgentResponse(
            new ChatAgentResponse("Answer", AnswerType.Grounded, []),
            newSession);

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.SessionId.Should().Be(newSession.ChatSessionId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsExistingSessionIdWhenProvided()
    {
        // Arrange
        var existingSession = ChatSession.Create(ValidSessionId, TimeSpan.FromHours(1));
        SetupExistingSessionScenario(existingSession);

        var request = new ChatRequest(ValidSessionId, ValidUserMessage);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.SessionId.Should().Be(ValidSessionId);
    }

    [Fact]
    public async Task HandleAsync_AnswerTextPropagatesUnchanged()
    {
        // Arrange
        const string expectedAnswer = "This is the exact answer text from the agent.";
        var agentResponse = new ChatAgentResponse(expectedAnswer, AnswerType.Grounded, []);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        result.AnswerText.Should().Be(expectedAnswer);
    }

    [Fact]
    public async Task HandleAsync_CallsBoundaryChatCompletedOnSuccess()
    {
        // Arrange
        var agentResponse = new ChatAgentResponse(ValidAnswerText, AnswerType.Grounded, []);
        SetupNewSessionWithAgentResponse(agentResponse);

        var request = new ChatRequest(null, ValidUserMessage);

        // Act
        await _sut.HandleAsync(request);

        // Assert
        _boundaryMock.Verify(
            b => b.ChatCompleted(It.Is<ChatResponse>(r =>
                r.AnswerText == ValidAnswerText &&
                r.AnswerType == AnswerType.Grounded)),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupNewSessionScenario(ChatSession session)
    {
        _sessionStoreMock
            .Setup(s => s.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _sessionStoreMock
            .Setup(s => s.AppendMessageAsync(It.IsAny<string>(), It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _chatAgentMock
            .Setup(a => a.GenerateResponseAsync(
                It.IsAny<IReadOnlyList<ChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatAgentResponse(ValidAnswerText, AnswerType.Grounded, []));
    }

    private void SetupNewSessionWithAgentResponse(ChatAgentResponse agentResponse, ChatSession? session = null)
    {
        session ??= ChatSession.Create(TimeSpan.FromHours(1));

        _sessionStoreMock
            .Setup(s => s.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _sessionStoreMock
            .Setup(s => s.AppendMessageAsync(It.IsAny<string>(), It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _chatAgentMock
            .Setup(a => a.GenerateResponseAsync(
                It.IsAny<IReadOnlyList<ChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agentResponse);
    }

    private void SetupExistingSessionScenario(ChatSession session)
    {
        _sessionStoreMock
            .Setup(s => s.GetSessionAsync(session.ChatSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _sessionStoreMock
            .Setup(s => s.GetMessagesAsync(session.ChatSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        _sessionStoreMock
            .Setup(s => s.AppendMessageAsync(It.IsAny<string>(), It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _chatAgentMock
            .Setup(a => a.GenerateResponseAsync(
                It.IsAny<IReadOnlyList<ChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatAgentResponse(ValidAnswerText, AnswerType.Grounded, []));
    }

    #endregion
}
