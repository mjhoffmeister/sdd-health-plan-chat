using FluentAssertions;
using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.UnitTests.Domain.Chat;

public sealed class RetrievedChunkTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateRetrievedChunk()
    {
        // Arrange & Act
        var chunk = new RetrievedChunk(
            ChunkId: "chunk-001",
            PlanDocumentId: "doc-123",
            PlanName: "Contoso Health PPO Silver",
            Section: "Preventive Care",
            Text: "Annual wellness visits are covered at 100%.",
            PageOrAnchor: "page-12",
            Score: 0.95);

        // Assert
        chunk.ChunkId.Should().Be("chunk-001");
    }

    [Fact]
    public void ChunkId_ShouldReturnExpectedValue()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Assert
        chunk.ChunkId.Should().Be("chunk-001");
    }

    [Fact]
    public void PlanDocumentId_ShouldReturnExpectedValue()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Assert
        chunk.PlanDocumentId.Should().Be("doc-123");
    }

    [Fact]
    public void PlanName_ShouldReturnExpectedValue()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Assert
        chunk.PlanName.Should().Be("Contoso Health PPO Silver");
    }

    [Fact]
    public void Section_ShouldReturnExpectedValue()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Assert
        chunk.Section.Should().Be("Preventive Care");
    }

    [Fact]
    public void Text_ShouldReturnExpectedValue()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Assert
        chunk.Text.Should().Be("Annual wellness visits are covered at 100%.");
    }

    [Fact]
    public void PageOrAnchor_ShouldReturnExpectedValue()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Assert
        chunk.PageOrAnchor.Should().Be("page-12");
    }

    [Fact]
    public void Score_ShouldReturnExpectedValue()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Assert
        chunk.Score.Should().Be(0.95);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var chunk1 = CreateTestChunk();
        var chunk2 = CreateTestChunk();

        // Assert
        chunk1.Should().Be(chunk2);
    }

    [Fact]
    public void Equality_WithDifferentChunkId_ShouldNotBeEqual()
    {
        // Arrange
        var chunk1 = CreateTestChunk();
        var chunk2 = CreateTestChunk() with { ChunkId = "chunk-002" };

        // Assert
        chunk1.Should().NotBe(chunk2);
    }

    [Fact]
    public void Equality_WithDifferentScore_ShouldNotBeEqual()
    {
        // Arrange
        var chunk1 = CreateTestChunk();
        var chunk2 = CreateTestChunk() with { Score = 0.80 };

        // Assert
        chunk1.Should().NotBe(chunk2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var chunk1 = CreateTestChunk();
        var chunk2 = CreateTestChunk();

        // Assert
        chunk1.GetHashCode().Should().Be(chunk2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHash()
    {
        // Arrange
        var chunk1 = CreateTestChunk();
        var chunk2 = CreateTestChunk() with { ChunkId = "chunk-999" };

        // Assert
        chunk1.GetHashCode().Should().NotBe(chunk2.GetHashCode());
    }

    // Record with expression tests for all properties

    [Fact]
    public void WithExpression_ChangingPlanDocumentId_ShouldCreateNewChunk()
    {
        // Arrange
        var original = CreateTestChunk();

        // Act
        var modified = original with { PlanDocumentId = "new-doc-id" };

        // Assert
        modified.PlanDocumentId.Should().Be("new-doc-id");
        modified.ChunkId.Should().Be(original.ChunkId);
    }

    [Fact]
    public void WithExpression_ChangingPlanName_ShouldCreateNewChunk()
    {
        // Arrange
        var original = CreateTestChunk();

        // Act
        var modified = original with { PlanName = "Contoso HMO Gold" };

        // Assert
        modified.PlanName.Should().Be("Contoso HMO Gold");
    }

    [Fact]
    public void WithExpression_ChangingSection_ShouldCreateNewChunk()
    {
        // Arrange
        var original = CreateTestChunk();

        // Act
        var modified = original with { Section = "Emergency Care" };

        // Assert
        modified.Section.Should().Be("Emergency Care");
    }

    [Fact]
    public void WithExpression_ChangingText_ShouldCreateNewChunk()
    {
        // Arrange
        var original = CreateTestChunk();

        // Act
        var modified = original with { Text = "New content text" };

        // Assert
        modified.Text.Should().Be("New content text");
    }

    [Fact]
    public void WithExpression_ChangingPageOrAnchor_ShouldCreateNewChunk()
    {
        // Arrange
        var original = CreateTestChunk();

        // Act
        var modified = original with { PageOrAnchor = "page-99" };

        // Assert
        modified.PageOrAnchor.Should().Be("page-99");
    }

    [Fact]
    public void WithExpression_ShouldNotModifyOriginal()
    {
        // Arrange
        var original = CreateTestChunk();

        // Act
        _ = original with { Score = 0.50 };

        // Assert
        original.Score.Should().Be(0.95);
    }

    // Score edge case tests

    [Fact]
    public void Score_WithZeroValue_ShouldBeValid()
    {
        // Act
        var chunk = CreateTestChunk() with { Score = 0.0 };

        // Assert
        chunk.Score.Should().Be(0.0);
    }

    [Fact]
    public void Score_WithOneValue_ShouldBeValid()
    {
        // Act
        var chunk = CreateTestChunk() with { Score = 1.0 };

        // Assert
        chunk.Score.Should().Be(1.0);
    }

    [Fact]
    public void Score_WithNegativeValue_ShouldBeAllowed()
    {
        // Act (no domain validation, just record creation)
        var chunk = CreateTestChunk() with { Score = -0.5 };

        // Assert
        chunk.Score.Should().Be(-0.5);
    }

    // ToString test

    [Fact]
    public void ToString_ShouldContainChunkId()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Act
        var result = chunk.ToString();

        // Assert
        result.Should().Contain("chunk-001");
    }

    [Fact]
    public void ToString_ShouldContainPlanName()
    {
        // Arrange
        var chunk = CreateTestChunk();

        // Act
        var result = chunk.ToString();

        // Assert
        result.Should().Contain("Contoso Health PPO Silver");
    }

    private static RetrievedChunk CreateTestChunk() => new(
        ChunkId: "chunk-001",
        PlanDocumentId: "doc-123",
        PlanName: "Contoso Health PPO Silver",
        Section: "Preventive Care",
        Text: "Annual wellness visits are covered at 100%.",
        PageOrAnchor: "page-12",
        Score: 0.95);
}
