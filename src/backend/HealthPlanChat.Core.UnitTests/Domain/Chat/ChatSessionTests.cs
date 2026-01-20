using FluentAssertions;
using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.UnitTests.Domain.Chat;

public sealed class ChatSessionTests
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    // Create with TTL tests

    [Fact]
    public void Create_WithTtl_ShouldCreateSession()
    {
        // Act
        var session = ChatSession.Create(DefaultTtl);

        // Assert
        session.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithTtl_ShouldGenerateSessionId()
    {
        // Act
        var session = ChatSession.Create(DefaultTtl);

        // Assert
        session.ChatSessionId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_WithTtl_ShouldSetCreatedAtUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var session = ChatSession.Create(DefaultTtl);

        // Assert
        session.CreatedAtUtc.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Create_WithTtl_ShouldSetLastUpdatedAtUtc()
    {
        // Act
        var session = ChatSession.Create(DefaultTtl);

        // Assert
        session.LastUpdatedAtUtc.Should().Be(session.CreatedAtUtc);
    }

    [Fact]
    public void Create_WithTtl_ShouldSetExpiresAtUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var session = ChatSession.Create(DefaultTtl);

        // Assert
        session.ExpiresAtUtc.Should().BeCloseTo(before.Add(DefaultTtl), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithTtl_ShouldStartWithEmptyMessages()
    {
        // Act
        var session = ChatSession.Create(DefaultTtl);

        // Assert
        session.Messages.Should().BeEmpty();
    }

    // Create with session ID and TTL tests

    [Fact]
    public void Create_WithSessionIdAndTtl_ShouldCreateSession()
    {
        // Arrange
        var sessionId = "custom-session-123";

        // Act
        var session = ChatSession.Create(sessionId, DefaultTtl);

        // Assert
        session.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithSessionIdAndTtl_ShouldUseProvidedSessionId()
    {
        // Arrange
        var sessionId = "custom-session-123";

        // Act
        var session = ChatSession.Create(sessionId, DefaultTtl);

        // Assert
        session.ChatSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void Create_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatSession.Create(null!, DefaultTtl);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptySessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatSession.Create(string.Empty, DefaultTtl);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceSessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatSession.Create("   ", DefaultTtl);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // CreateWithMessages tests

    [Fact]
    public void CreateWithMessages_WithValidInput_ShouldCreateSession()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.Should().NotBeNull();
    }

    [Fact]
    public void CreateWithMessages_ShouldSetSessionId()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.ChatSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void CreateWithMessages_ShouldSetCreatedAtUtc()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.CreatedAtUtc.Should().Be(createdAt);
    }

    [Fact]
    public void CreateWithMessages_ShouldSetExpiresAtUtc()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.ExpiresAtUtc.Should().Be(expiresAt);
    }

    [Fact]
    public void CreateWithMessages_WithEmptyMessages_ShouldSetLastUpdatedToCreatedAt()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.LastUpdatedAtUtc.Should().Be(createdAt);
    }

    [Fact]
    public void CreateWithMessages_WithMessages_ShouldSetLastUpdatedToLatestMessage()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(sessionId, "First message"),
            ChatMessage.CreateAssistantMessage(sessionId, "Response")
        };
        var latestMessageTime = messages.Max(m => m.CreatedAtUtc);

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.LastUpdatedAtUtc.Should().Be(latestMessageTime);
    }

    [Fact]
    public void CreateWithMessages_ShouldRestoreMessages()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(sessionId, "First message"),
            ChatMessage.CreateAssistantMessage(sessionId, "Response")
        };

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.Messages.Should().HaveCount(2);
    }

    [Fact]
    public void CreateWithMessages_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var act = () => ChatSession.CreateWithMessages(null!, createdAt, expiresAt, messages);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // AddMessage tests

    [Fact]
    public void AddMessage_WithValidMessage_ShouldAddToMessages()
    {
        // Arrange
        var session = ChatSession.Create(DefaultTtl);
        var message = ChatMessage.CreateUserMessage(session.ChatSessionId, "Hello");

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages.Should().ContainSingle();
    }

    [Fact]
    public void AddMessage_ShouldUpdateLastUpdatedAtUtc()
    {
        // Arrange
        var session = ChatSession.Create(DefaultTtl);
        var originalLastUpdated = session.LastUpdatedAtUtc;
        var message = ChatMessage.CreateUserMessage(session.ChatSessionId, "Hello");

        // Small delay to ensure time difference
        Thread.Sleep(10);

        // Act
        session.AddMessage(message);

        // Assert
        session.LastUpdatedAtUtc.Should().BeAfter(originalLastUpdated);
    }

    [Fact]
    public void AddMessage_CalledMultipleTimes_ShouldMaintainOrder()
    {
        // Arrange
        var session = ChatSession.Create(DefaultTtl);
        var userMessage = ChatMessage.CreateUserMessage(session.ChatSessionId, "Question");
        var assistantMessage = ChatMessage.CreateAssistantMessage(session.ChatSessionId, "Answer");

        // Act
        session.AddMessage(userMessage);
        session.AddMessage(assistantMessage);

        // Assert
        session.Messages[0].Role.Should().Be(Role.User);
        session.Messages[1].Role.Should().Be(Role.Assistant);
    }

    [Fact]
    public void AddMessage_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var session = ChatSession.Create(DefaultTtl);

        // Act
        var act = () => session.AddMessage(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // Messages collection immutability test

    [Fact]
    public void Messages_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var session = ChatSession.Create(DefaultTtl);

        // Assert
        session.Messages.Should().BeAssignableTo<IReadOnlyList<ChatMessage>>();
    }

    // Session ID uniqueness test

    [Fact]
    public void Create_CalledTwice_ShouldGenerateDifferentSessionIds()
    {
        // Act
        var session1 = ChatSession.Create(DefaultTtl);
        var session2 = ChatSession.Create(DefaultTtl);

        // Assert
        session1.ChatSessionId.Should().NotBe(session2.ChatSessionId);
    }

    // Session ID format test

    [Fact]
    public void Create_ShouldGenerateSessionIdWithoutHyphens()
    {
        // Act
        var session = ChatSession.Create(DefaultTtl);

        // Assert (GUID "N" format has no hyphens and is 32 characters)
        session.ChatSessionId.Should().HaveLength(32);
        session.ChatSessionId.Should().NotContain("-");
    }

    // TTL edge case tests

    [Fact]
    public void Create_WithZeroTtl_ShouldSetExpiresAtToCreatedAt()
    {
        // Act
        var session = ChatSession.Create(TimeSpan.Zero);

        // Assert
        session.ExpiresAtUtc.Should().Be(session.CreatedAtUtc);
    }

    [Fact]
    public void Create_WithNegativeTtl_ShouldSetExpiresAtBeforeCreatedAt()
    {
        // Act
        var session = ChatSession.Create(TimeSpan.FromHours(-1));

        // Assert
        session.ExpiresAtUtc.Should().BeBefore(session.CreatedAtUtc);
    }

    // CreateWithMessages additional validation tests

    [Fact]
    public void CreateWithMessages_WithEmptySessionId_ShouldThrowArgumentException()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var act = () => ChatSession.CreateWithMessages(string.Empty, createdAt, expiresAt, messages);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithMessages_WithWhitespaceSessionId_ShouldThrowArgumentException()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>();

        // Act
        var act = () => ChatSession.CreateWithMessages("   ", createdAt, expiresAt, messages);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithMessages_ShouldPreserveMessageOrder()
    {
        // Arrange
        var sessionId = "session-123";
        var createdAt = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(sessionId, "First"),
            ChatMessage.CreateAssistantMessage(sessionId, "Second"),
            ChatMessage.CreateUserMessage(sessionId, "Third")
        };

        // Act
        var session = ChatSession.CreateWithMessages(sessionId, createdAt, expiresAt, messages);

        // Assert
        session.Messages[0].Text.Should().Be("First");
        session.Messages[1].Text.Should().Be("Second");
        session.Messages[2].Text.Should().Be("Third");
    }

    // AddMessage content preservation test

    [Fact]
    public void AddMessage_ShouldPreserveMessageContent()
    {
        // Arrange
        var session = ChatSession.Create(DefaultTtl);
        var message = ChatMessage.CreateUserMessage(session.ChatSessionId, "Test message");

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages[0].Text.Should().Be("Test message");
        session.Messages[0].ChatMessageId.Should().Be(message.ChatMessageId);
    }
}
