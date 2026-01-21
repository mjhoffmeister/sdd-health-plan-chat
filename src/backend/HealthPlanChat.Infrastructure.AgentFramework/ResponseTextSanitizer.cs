using System.Text.RegularExpressions;

namespace HealthPlanChat.Infrastructure.AgentFramework;

/// <summary>
/// Sanitizes agent response text by removing answer type labels and citation markers.
/// The AnswerType and References are already extracted separately, so these markers
/// should not appear in the final user-visible text.
/// </summary>
public static partial class ResponseTextSanitizer
{
    // Patterns for answer type labels (with optional markdown formatting)
    // Matches: **[GROUNDED]**, *[GROUNDED]*, [GROUNDED], **[GENERAL GUIDANCE]**, etc.
    [GeneratedRegex(@"\*{0,2}\[GROUNDED\]\*{0,2}", RegexOptions.IgnoreCase)]
    private static partial Regex GroundedLabelPattern();

    [GeneratedRegex(@"\*{0,2}\[GENERAL GUIDANCE\]\*{0,2}", RegexOptions.IgnoreCase)]
    private static partial Regex GeneralGuidanceLabelPattern();

    // Pattern for citation markers like 【3:0†source】, 【1:2†source】, etc.
    // Unicode brackets: 【 (U+3010) and 】 (U+3011)
    [GeneratedRegex(@"【\d+:\d+†[^】]*】")]
    private static partial Regex CitationMarkerPattern();

    // Pattern for alternative citation formats like [doc_0], [doc_1], etc.
    [GeneratedRegex(@"\[doc_\d+\]")]
    private static partial Regex DocCitationPattern();

    // Pattern for numbered citation markers like [1], [2], [3] at the end of sentences
    // Only match standalone citation numbers, not markdown links
    [GeneratedRegex(@"(?<!\])\[\d+\](?!\()")]
    private static partial Regex NumberedCitationPattern();

    /// <summary>
    /// Sanitizes the response text by removing answer type labels and citation markers.
    /// </summary>
    /// <param name="responseText">The raw response text from the agent.</param>
    /// <returns>Sanitized text suitable for display to users.</returns>
    public static string Sanitize(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
        {
            return responseText;
        }

        var result = responseText;

        // Remove answer type labels
        result = GroundedLabelPattern().Replace(result, string.Empty);
        result = GeneralGuidanceLabelPattern().Replace(result, string.Empty);

        // Remove citation markers (with optional preceding space)
        result = Regex.Replace(result, @"\s*【\d+:\d+†[^】]*】", string.Empty);
        result = Regex.Replace(result, @"\s*\[doc_\d+\]", string.Empty);
        result = Regex.Replace(result, @"\s*(?<!\])\[\d+\](?!\()", string.Empty);

        // Clean up any resulting double spaces or leading/trailing whitespace
        result = Regex.Replace(result, @"[ \t]{2,}", " ");
        result = Regex.Replace(result, @"^\s+", string.Empty, RegexOptions.Multiline);
        result = result.Trim();

        return result;
    }

    /// <summary>
    /// Checks if the response text contains a grounded label.
    /// </summary>
    public static bool ContainsGroundedLabel(string responseText)
    {
        return !string.IsNullOrWhiteSpace(responseText) &&
               GroundedLabelPattern().IsMatch(responseText);
    }

    /// <summary>
    /// Checks if the response text contains a general guidance label.
    /// </summary>
    public static bool ContainsGeneralGuidanceLabel(string responseText)
    {
        return !string.IsNullOrWhiteSpace(responseText) &&
               GeneralGuidanceLabelPattern().IsMatch(responseText);
    }
}
