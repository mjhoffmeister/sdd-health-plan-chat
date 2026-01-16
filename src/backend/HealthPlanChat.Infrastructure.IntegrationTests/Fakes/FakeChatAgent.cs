using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Core.UseCases.Contracts;

namespace HealthPlanChat.Infrastructure.IntegrationTests.Fakes;

/// <summary>
/// Fake implementation of IChatAgent for testing.
/// Implements deterministic behavior that mirrors real agent logic:
/// - Returns Grounded with references when relevant chunks are provided
/// - Returns GeneralGuidance with no references when no chunks are available
/// </summary>
public sealed class FakeChatAgent : IChatAgent
{
    private const double MinScoreForGrounding = 0.5;

    public Task<ChatAgentResponse> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        IReadOnlyList<RetrievedChunk> retrievedChunks,
        CancellationToken cancellationToken = default)
    {
        // Determine if we have sufficient grounding material
        var relevantChunks = retrievedChunks
            .Where(c => c.Score >= MinScoreForGrounding)
            .ToList();

        if (relevantChunks.Count == 0)
        {
            // No relevant chunks - return general guidance
            return Task.FromResult(new ChatAgentResponse(
                AnswerText: "I don't have specific information about that in your plan documents. " +
                           "Please consult your plan materials or contact member services for accurate information.",
                AnswerType: AnswerType.GeneralGuidance,
                References: []));
        }

        // Build grounded response from chunks
        var topChunk = relevantChunks.First();
        var answerText = BuildAnswerFromChunks(userMessage, relevantChunks);
        var references = BuildReferences(relevantChunks);

        return Task.FromResult(new ChatAgentResponse(
            AnswerText: answerText,
            AnswerType: AnswerType.Grounded,
            References: references));
    }

    private static string BuildAnswerFromChunks(string question, List<RetrievedChunk> chunks)
    {
        var topChunk = chunks.First();

        // Simulate an LLM synthesizing an answer from chunks
        // In reality, the LLM would paraphrase; here we just confirm we're using the content
        return $"Based on your {topChunk.PlanName}, here's what I found: {topChunk.Text}";
    }

    private static IReadOnlyList<Reference> BuildReferences(List<RetrievedChunk> chunks)
    {
        // Create references from the chunks used for grounding
        return chunks
            .Select(chunk => new Reference(
                PlanDocumentId: chunk.PlanDocumentId,
                Anchor: chunk.PageOrAnchor,
                Quote: TruncateQuote(chunk.Text)))
            .ToList();
    }

    private static string TruncateQuote(string text)
    {
        // Truncate quote to reasonable length (like a real citation)
        const int maxLength = 100;
        if (text.Length <= maxLength)
            return text;

        var truncated = text[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 50)
            truncated = truncated[..lastSpace];

        return truncated + "...";
    }
}
