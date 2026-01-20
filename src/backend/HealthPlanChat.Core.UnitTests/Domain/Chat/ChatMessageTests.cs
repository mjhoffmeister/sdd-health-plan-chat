using FluentAssertions;
using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.UnitTests.Domain.Chat;

public sealed class ChatMessageTests
{
    private const string ValidSessionId = "session-123";
    private const string ValidText = "What is my deductible?";

    // CreateUserMessage tests

    [Fact]
    public void CreateUserMessage_WithValidInput_ShouldCreateMessage()
    {
        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert
        message.Should().NotBeNull();
    }

    [Fact]
    public void CreateUserMessage_ShouldSetRoleToUser()
    {
        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert
        message.Role.Should().Be(Role.User);
    }

    [Fact]
    public void CreateUserMessage_ShouldSetText()
    {
        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert
        message.Text.Should().Be(ValidText);
    }

    [Fact]
    public void CreateUserMessage_ShouldSetChatSessionId()
    {
        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert
        message.ChatSessionId.Should().Be(ValidSessionId);
    }

    [Fact]
    public void CreateUserMessage_ShouldGenerateUniqueMessageId()
    {
        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert
        message.ChatMessageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateUserMessage_ShouldSetCreatedAtUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert
        message.CreatedAtUtc.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void CreateUserMessage_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateUserMessage(null!, ValidText);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateUserMessage_WithEmptySessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateUserMessage(string.Empty, ValidText);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateUserMessage_WithWhitespaceSessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateUserMessage("   ", ValidText);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateUserMessage_WithNullText_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateUserMessage(ValidSessionId, null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateUserMessage_WithEmptyText_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateUserMessage(ValidSessionId, string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateUserMessage_WithWhitespaceText_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateUserMessage(ValidSessionId, "   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // CreateAssistantMessage tests

    [Fact]
    public void CreateAssistantMessage_WithValidInput_ShouldCreateMessage()
    {
        // Arrange
        var assistantText = "Your deductible is $500.";

        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);

        // Assert
        message.Should().NotBeNull();
    }

    [Fact]
    public void CreateAssistantMessage_ShouldSetRoleToAssistant()
    {
        // Arrange
        var assistantText = "Your deductible is $500.";

        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);

        // Assert
        message.Role.Should().Be(Role.Assistant);
    }

    [Fact]
    public void CreateAssistantMessage_ShouldSetText()
    {
        // Arrange
        var assistantText = "Your deductible is $500.";

        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);

        // Assert
        message.Text.Should().Be(assistantText);
    }

    [Fact]
    public void CreateAssistantMessage_ShouldSetChatSessionId()
    {
        // Arrange
        var assistantText = "Your deductible is $500.";

        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);

        // Assert
        message.ChatSessionId.Should().Be(ValidSessionId);
    }

    [Fact]
    public void CreateAssistantMessage_ShouldGenerateUniqueMessageId()
    {
        // Arrange
        var assistantText = "Your deductible is $500.";

        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);

        // Assert
        message.ChatMessageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateAssistantMessage_ShouldSetCreatedAtUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var assistantText = "Your deductible is $500.";

        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);

        // Assert
        message.CreatedAtUtc.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void CreateAssistantMessage_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateAssistantMessage(null!, "Response text");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAssistantMessage_WithEmptySessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateAssistantMessage(string.Empty, "Response text");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAssistantMessage_WithWhitespaceSessionId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateAssistantMessage("   ", "Response text");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAssistantMessage_WithNullText_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateAssistantMessage(ValidSessionId, null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAssistantMessage_WithEmptyText_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateAssistantMessage(ValidSessionId, string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAssistantMessage_WithWhitespaceText_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ChatMessage.CreateAssistantMessage(ValidSessionId, "   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // MessageId uniqueness test

    [Fact]
    public void CreateUserMessage_CalledTwice_ShouldGenerateDifferentMessageIds()
    {
        // Act
        var message1 = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);
        var message2 = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert
        message1.ChatMessageId.Should().NotBe(message2.ChatMessageId);
    }

    [Fact]
    public void CreateAssistantMessage_CalledTwice_ShouldGenerateDifferentMessageIds()
    {
        // Arrange
        var assistantText = "Your deductible is $500.";

        // Act
        var message1 = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);
        var message2 = ChatMessage.CreateAssistantMessage(ValidSessionId, assistantText);

        // Assert
        message1.ChatMessageId.Should().NotBe(message2.ChatMessageId);
    }

    // MessageId format test

    [Fact]
    public void CreateUserMessage_ShouldGenerateMessageIdWithout_Hyphens()
    {
        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);

        // Assert (GUID "N" format has no hyphens and is 32 characters)
        message.ChatMessageId.Should().HaveLength(32);
        message.ChatMessageId.Should().NotContain("-");
    }

    [Fact]
    public void CreateAssistantMessage_ShouldGenerateMessageIdWithoutHyphens()
    {
        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, "Response");

        // Assert (GUID "N" format has no hyphens and is 32 characters)
        message.ChatMessageId.Should().HaveLength(32);
        message.ChatMessageId.Should().NotContain("-");
    }

    // CreatedAtUtc precision tests

    [Fact]
    public void CreateUserMessage_ShouldSetCreatedAtUtcBeforeNow()
    {
        // Act
        var message = ChatMessage.CreateUserMessage(ValidSessionId, ValidText);
        var after = DateTime.UtcNow;

        // Assert
        message.CreatedAtUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void CreateAssistantMessage_ShouldSetCreatedAtUtcBeforeNow()
    {
        // Act
        var message = ChatMessage.CreateAssistantMessage(ValidSessionId, "Response");
        var after = DateTime.UtcNow;

        // Assert
        message.CreatedAtUtc.Should().BeOnOrBefore(after);
    }
}
