using FluentAssertions;
using HealthPlanChat.Infrastructure.Prompting;

namespace HealthPlanChat.Infrastructure.Prompting.UnitTests;

/// <summary>
/// Unit tests for PromptBuilder.
/// Tests citation formatting, labeling rules, and prompt structure.
/// </summary>
public sealed class PromptBuilderTests
{
    private readonly PromptBuilder _sut = new();

    [Fact]
    public void BuildSystemPrompt_ShouldReturnNonEmptyString()
    {
        // Act
        var systemPrompt = _sut.BuildSystemPrompt();

        // Assert
        systemPrompt.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BuildSystemPrompt_ShouldContainGroundingInstructions()
    {
        // Act
        var systemPrompt = _sut.BuildSystemPrompt();

        // Assert: System prompt should instruct the model about grounding
        systemPrompt.Should().ContainAny("GROUNDED", "search", "citation");
    }

    [Fact]
    public void BuildSystemPrompt_ShouldContainAnswerTypeLabeling()
    {
        // Act
        var systemPrompt = _sut.BuildSystemPrompt();

        // Assert: System prompt should mention both answer type labels
        systemPrompt.Should().Contain("GROUNDED");
        systemPrompt.Should().Contain("GENERAL GUIDANCE");
    }

    [Fact]
    public void BuildSystemPrompt_ShouldContainHealthPlanContext()
    {
        // Act
        var systemPrompt = _sut.BuildSystemPrompt();

        // Assert: Should establish health plan assistant context
        systemPrompt.Should().ContainAny("health", "plan", "insurance", "benefits");
    }

    [Fact]
    public void BuildSystemPrompt_ShouldBeConsistentAcrossCalls()
    {
        // Act
        var prompt1 = _sut.BuildSystemPrompt();
        var prompt2 = _sut.BuildSystemPrompt();

        // Assert: Same prompt should be returned each time
        prompt1.Should().Be(prompt2);
    }

    [Fact]
    public void BuildSystemPrompt_ShouldContainCitationFormat()
    {
        // Act
        var systemPrompt = _sut.BuildSystemPrompt();

        // Assert: Should include citation format instructions
        systemPrompt.Should().Contain("Source:");
    }

    [Fact]
    public void BuildSystemPrompt_ShouldContainSafetyInstructions()
    {
        // Act
        var systemPrompt = _sut.BuildSystemPrompt();

        // Assert: Should include safety guidance
        systemPrompt.Should().ContainAny("Safety", "healthcare professionals", "diagnoses");
    }
}
