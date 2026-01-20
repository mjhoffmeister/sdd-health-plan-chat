using HealthPlanChat.Core.Domain.Chat;

namespace HealthPlanChat.Core.Domain.Retrieval;

/// <summary>
/// Retrieval policy that determines whether retrieved chunks meet the confidence threshold
/// for a grounded response vs. general guidance.
/// </summary>
/// <remarks>
/// <para>
/// <b>DEPRECATED:</b> This class is deprecated as of the agent-native RAG refactor.
/// The agent now handles grounding decisions internally using the Azure AI Search tool.
/// The agent's system prompt instructs it to determine [GROUNDED] vs [GENERAL GUIDANCE]
/// based on search result quality.
/// </para>
/// <para>
/// Configuration keys (no longer used):
/// - Retrieval__MinHits: Minimum number of hits required (default: 1)
/// - Retrieval__MinTopScore: Minimum top score threshold (default: 0.7)
/// </para>
/// </remarks>
[Obsolete("Agent now handles grounding decisions internally via AzureAISearchAgentTool. Use agent prompt instructions instead.")]
public sealed class RetrievalPolicy
{
    /// <summary>
    /// Default minimum number of hits for confidence.
    /// </summary>
    public const int DefaultMinHits = 1;

    /// <summary>
    /// Default minimum top score for confidence.
    /// </summary>
    public const double DefaultMinTopScore = 0.7;

    /// <summary>
    /// Minimum number of hits required for grounded response.
    /// </summary>
    public int MinHits { get; }

    /// <summary>
    /// Minimum top score required for grounded response.
    /// </summary>
    public double MinTopScore { get; }

    /// <summary>
    /// Creates a new retrieval policy with the specified thresholds.
    /// </summary>
    /// <param name="minHits">Minimum number of hits required. Default: 1.</param>
    /// <param name="minTopScore">Minimum top score required. Default: 0.7.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minHits is less than 0 or minTopScore is not between 0 and 1.</exception>
    public RetrievalPolicy(int minHits = DefaultMinHits, double minTopScore = DefaultMinTopScore)
    {
        if (minHits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minHits), "MinHits must be non-negative.");
        }

        if (minTopScore < 0 || minTopScore > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minTopScore), "MinTopScore must be between 0 and 1.");
        }

        MinHits = minHits;
        MinTopScore = minTopScore;
    }

    /// <summary>
    /// Evaluates whether the retrieved chunks meet the confidence threshold for grounding.
    /// </summary>
    /// <param name="retrievedChunks">The retrieved chunks to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the chunks meet the confidence threshold (answer can be grounded);
    /// <c>false</c> if the chunks do not meet the threshold (should use general guidance).
    /// </returns>
    /// <remarks>
    /// Confidence is defined as:
    /// - Has at least <see cref="MinHits"/> chunks AND
    /// - Top AI Search score >= <see cref="MinTopScore"/>
    /// </remarks>
    public bool MeetsConfidenceThreshold(IReadOnlyList<RetrievedChunk> retrievedChunks)
    {
        ArgumentNullException.ThrowIfNull(retrievedChunks);

        // Check minimum hit count
        if (retrievedChunks.Count < MinHits)
        {
            return false;
        }

        // If no minimum hits required and we have empty results, that's still not confident
        if (retrievedChunks.Count == 0)
        {
            return false;
        }

        // Check top score threshold (chunks are expected to be ordered by score descending)
        var topScore = retrievedChunks.Max(c => c.Score);
        return topScore >= MinTopScore;
    }

    /// <summary>
    /// Creates a default retrieval policy.
    /// </summary>
    public static RetrievalPolicy Default => new();
}
