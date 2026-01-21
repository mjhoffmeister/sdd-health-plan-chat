namespace HealthPlanChat.Infrastructure.AgentFramework.UnitTests;

public class ResponseTextSanitizerTests
{
    [Fact]
    public void Sanitize_NullInput_ReturnsNull()
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Sanitize_EmptyInput_ReturnsEmpty()
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_WhitespaceInput_ReturnsEmpty()
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("**[GROUNDED]** Here is the answer.", "Here is the answer.")]
    [InlineData("[GROUNDED] Here is the answer.", "Here is the answer.")]
    [InlineData("*[GROUNDED]* Here is the answer.", "Here is the answer.")]
    [InlineData("**[grounded]** Here is the answer.", "Here is the answer.")]
    public void Sanitize_StripsGroundedLabel(string input, string expected)
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("**[GENERAL GUIDANCE]** I cannot find that info.", "I cannot find that info.")]
    [InlineData("[GENERAL GUIDANCE] I cannot find that info.", "I cannot find that info.")]
    [InlineData("*[GENERAL GUIDANCE]* I cannot find that info.", "I cannot find that info.")]
    [InlineData("**[general guidance]** I cannot find that info.", "I cannot find that info.")]
    public void Sanitize_StripsGeneralGuidanceLabel(string input, string expected)
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("The deductible is $500【3:0†source】.", "The deductible is $500.")]
    [InlineData("Coverage includes therapy【1:2†source】 and checkups【2:1†source】.", "Coverage includes therapy and checkups.")]
    [InlineData("See plan details【10:15†source】.", "See plan details.")]
    public void Sanitize_StripsCitationMarkers(string input, string expected)
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("The deductible is $500 [doc_0].", "The deductible is $500.")]
    [InlineData("Coverage [doc_1] and benefits [doc_2].", "Coverage and benefits.")]
    public void Sanitize_StripsDocCitationMarkers(string input, string expected)
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("The deductible is $500 [1].", "The deductible is $500.")]
    [InlineData("Coverage [1] and benefits [2].", "Coverage and benefits.")]
    public void Sanitize_StripsNumberedCitationMarkers(string input, string expected)
    {
        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Sanitize_PreservesMarkdownLinks()
    {
        // Arrange
        var input = "See [this link](https://example.com) for details.";

        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert - markdown links should be preserved
        result.Should().Be("See [this link](https://example.com) for details.");
    }

    [Fact]
    public void Sanitize_CombinedLabelAndCitations()
    {
        // Arrange
        var input = "**[GROUNDED]** The deductible is $500【3:0†source】. Coverage includes therapy【1:2†source】.";
        var expected = "The deductible is $500. Coverage includes therapy.";

        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Sanitize_PreservesRegularMarkdown()
    {
        // Arrange
        var input = "The **deductible** is *important*.";

        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert - regular markdown should be preserved for frontend rendering
        result.Should().Be("The **deductible** is *important*.");
    }

    [Fact]
    public void Sanitize_CleansUpDoubleSpaces()
    {
        // Arrange
        var input = "**[GROUNDED]**  Here is  the answer.";

        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be("Here is the answer.");
    }

    [Fact]
    public void Sanitize_TrimsLeadingAndTrailingWhitespace()
    {
        // Arrange
        var input = "  **[GROUNDED]** Here is the answer.  ";

        // Act
        var result = ResponseTextSanitizer.Sanitize(input);

        // Assert
        result.Should().Be("Here is the answer.");
    }

    [Fact]
    public void ContainsGroundedLabel_ReturnsTrue_WhenPresent()
    {
        // Arrange
        var input = "**[GROUNDED]** Answer here.";

        // Act
        var result = ResponseTextSanitizer.ContainsGroundedLabel(input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsGroundedLabel_ReturnsFalse_WhenNotPresent()
    {
        // Arrange
        var input = "Just a regular answer.";

        // Act
        var result = ResponseTextSanitizer.ContainsGroundedLabel(input);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsGeneralGuidanceLabel_ReturnsTrue_WhenPresent()
    {
        // Arrange
        var input = "**[GENERAL GUIDANCE]** I cannot find that.";

        // Act
        var result = ResponseTextSanitizer.ContainsGeneralGuidanceLabel(input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsGeneralGuidanceLabel_ReturnsFalse_WhenNotPresent()
    {
        // Arrange
        var input = "Just a regular answer.";

        // Act
        var result = ResponseTextSanitizer.ContainsGeneralGuidanceLabel(input);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsGroundedLabel_ReturnsFalse_ForNullOrEmptyInput(string? input)
    {
        // Act
        var result = ResponseTextSanitizer.ContainsGroundedLabel(input!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsGeneralGuidanceLabel_ReturnsFalse_ForNullOrEmptyInput(string? input)
    {
        // Act
        var result = ResponseTextSanitizer.ContainsGeneralGuidanceLabel(input!);

        // Assert
        result.Should().BeFalse();
    }
}
