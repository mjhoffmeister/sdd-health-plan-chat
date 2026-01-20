using FluentAssertions;
using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.UnitTests.Domain.Chat;

public sealed class ReferenceTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateReference()
    {
        // Arrange & Act
        var reference = new Reference("doc-123", "page-5", "Coverage includes preventive care.");

        // Assert
        reference.PlanDocumentId.Should().Be("doc-123");
    }

    [Fact]
    public void PlanDocumentId_ShouldReturnExpectedValue()
    {
        // Arrange
        var reference = new Reference("doc-123", "section-2", "Quote text");

        // Assert
        reference.PlanDocumentId.Should().Be("doc-123");
    }

    [Fact]
    public void Anchor_ShouldReturnExpectedValue()
    {
        // Arrange
        var reference = new Reference("doc-123", "section-2", "Quote text");

        // Assert
        reference.Anchor.Should().Be("section-2");
    }

    [Fact]
    public void Quote_ShouldReturnExpectedValue()
    {
        // Arrange
        var reference = new Reference("doc-123", "section-2", "Quote text");

        // Assert
        reference.Quote.Should().Be("Quote text");
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var reference1 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");
        var reference2 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");

        // Assert
        reference1.Should().Be(reference2);
    }

    [Fact]
    public void Equality_WithDifferentPlanDocumentId_ShouldNotBeEqual()
    {
        // Arrange
        var reference1 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");
        var reference2 = new Reference("doc-456", "page-5", "Coverage includes preventive care.");

        // Assert
        reference1.Should().NotBe(reference2);
    }

    [Fact]
    public void Equality_WithDifferentAnchor_ShouldNotBeEqual()
    {
        // Arrange
        var reference1 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");
        var reference2 = new Reference("doc-123", "page-10", "Coverage includes preventive care.");

        // Assert
        reference1.Should().NotBe(reference2);
    }

    [Fact]
    public void Equality_WithDifferentQuote_ShouldNotBeEqual()
    {
        // Arrange
        var reference1 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");
        var reference2 = new Reference("doc-123", "page-5", "Different quote text.");

        // Assert
        reference1.Should().NotBe(reference2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var reference1 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");
        var reference2 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");

        // Assert
        reference1.GetHashCode().Should().Be(reference2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHash()
    {
        // Arrange
        var reference1 = new Reference("doc-123", "page-5", "Coverage includes preventive care.");
        var reference2 = new Reference("doc-456", "page-5", "Coverage includes preventive care.");

        // Assert
        reference1.GetHashCode().Should().NotBe(reference2.GetHashCode());
    }

    // Record with expression tests

    [Fact]
    public void WithExpression_ChangingPlanDocumentId_ShouldCreateNewReference()
    {
        // Arrange
        var original = new Reference("doc-123", "page-5", "Quote");

        // Act
        var modified = original with { PlanDocumentId = "doc-456" };

        // Assert
        modified.PlanDocumentId.Should().Be("doc-456");
        modified.Anchor.Should().Be("page-5");
        modified.Quote.Should().Be("Quote");
    }

    [Fact]
    public void WithExpression_ChangingAnchor_ShouldCreateNewReference()
    {
        // Arrange
        var original = new Reference("doc-123", "page-5", "Quote");

        // Act
        var modified = original with { Anchor = "section-10" };

        // Assert
        modified.Anchor.Should().Be("section-10");
    }

    [Fact]
    public void WithExpression_ChangingQuote_ShouldCreateNewReference()
    {
        // Arrange
        var original = new Reference("doc-123", "page-5", "Original Quote");

        // Act
        var modified = original with { Quote = "New Quote" };

        // Assert
        modified.Quote.Should().Be("New Quote");
    }

    [Fact]
    public void WithExpression_ShouldNotModifyOriginal()
    {
        // Arrange
        var original = new Reference("doc-123", "page-5", "Original");

        // Act
        _ = original with { PlanDocumentId = "doc-456" };

        // Assert
        original.PlanDocumentId.Should().Be("doc-123");
    }

    // Null/empty value handling tests

    [Fact]
    public void Constructor_WithEmptyPlanDocumentId_ShouldCreateReference()
    {
        // Act
        var reference = new Reference(string.Empty, "page-5", "Quote");

        // Assert
        reference.PlanDocumentId.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyAnchor_ShouldCreateReference()
    {
        // Act
        var reference = new Reference("doc-123", string.Empty, "Quote");

        // Assert
        reference.Anchor.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyQuote_ShouldCreateReference()
    {
        // Act
        var reference = new Reference("doc-123", "page-5", string.Empty);

        // Assert
        reference.Quote.Should().BeEmpty();
    }

    // ToString test

    [Fact]
    public void ToString_ShouldContainAllPropertyValues()
    {
        // Arrange
        var reference = new Reference("doc-123", "page-5", "Quote text");

        // Act
        var result = reference.ToString();

        // Assert
        result.Should().Contain("doc-123");
        result.Should().Contain("page-5");
        result.Should().Contain("Quote text");
    }
}
