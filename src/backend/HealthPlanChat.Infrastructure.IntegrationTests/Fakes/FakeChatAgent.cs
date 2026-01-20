using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;

namespace HealthPlanChat.Infrastructure.IntegrationTests.Fakes;

/// <summary>
/// Fake implementation of IChatAgent for testing.
/// Simulates agent-native search behavior:
/// - Analyzes the user message to determine if it's in-scope
/// - Returns Grounded with references for plan-related questions
/// - Returns GeneralGuidance with no references for out-of-scope questions
/// </summary>
public sealed class FakeChatAgent : IChatAgent
{
    // Keywords that indicate plan-related questions (simulates search results)
    private static readonly string[] PlanKeywords =
    [
        "deductible", "copay", "copayment", "coinsurance", "premium",
        "coverage", "benefit", "network", "provider", "claim",
        "prescription", "pharmacy", "emergency", "urgent care",
        "out-of-pocket", "hmo", "ppo", "epo", "plan"
    ];

    public Task<ChatAgentResponse> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        // Simulate agent using search tool internally
        // Check if the user's question relates to plan materials
        var isInScope = PlanKeywords.Any(keyword =>
            userMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (!isInScope)
        {
            // No relevant search results - return general guidance
            return Task.FromResult(new ChatAgentResponse(
                AnswerText: "**[GENERAL GUIDANCE]**\n\n" +
                           "I don't have specific information about that in your plan documents. " +
                           "Please consult your plan materials or contact member services for accurate information.",
                AnswerType: AnswerType.GeneralGuidance,
                References: []));
        }

        // Build grounded response (simulates finding relevant plan documents)
        var answerText = BuildGroundedAnswer(userMessage);
        var references = BuildReferences(userMessage);

        return Task.FromResult(new ChatAgentResponse(
            AnswerText: answerText,
            AnswerType: AnswerType.Grounded,
            References: references));
    }

    private static string BuildGroundedAnswer(string question)
    {
        // Simulate LLM synthesizing an answer from search results
        if (question.Contains("deductible", StringComparison.OrdinalIgnoreCase))
        {
            return "**[GROUNDED]**\n\n" +
                   "Based on your Contoso PPO Silver 2026 plan, your annual deductible is $2,000 for individual coverage " +
                   "and $4,000 for family coverage. The deductible applies to most covered services except for " +
                   "preventive care, which is covered at 100% before the deductible. [Source: Contoso PPO Silver 2026]";
        }

        if (question.Contains("copay", StringComparison.OrdinalIgnoreCase))
        {
            return "**[GROUNDED]**\n\n" +
                   "Your copay amounts depend on the type of service. For primary care visits, your copay is $25. " +
                   "Specialist visits have a $50 copay. Emergency room visits have a $250 copay, which is waived " +
                   "if you're admitted. [Source: Contoso PPO Silver 2026]";
        }

        // Generic plan-related answer
        return "**[GROUNDED]**\n\n" +
               "Based on your plan documents, I found relevant information about your coverage. " +
               "Your plan provides comprehensive benefits including medical, prescription drug, and " +
               "preventive care coverage. [Source: Contoso PPO Silver 2026]";
    }

    private static IReadOnlyList<Reference> BuildReferences(string question)
    {
        // Simulate citations extracted from agent response annotations
        var references = new List<Reference>();

        if (question.Contains("deductible", StringComparison.OrdinalIgnoreCase))
        {
            references.Add(new Reference(
                PlanDocumentId: "contoso-ppo-silver-2026",
                Anchor: "Cost Sharing",
                Quote: "Annual deductible: $2,000 individual / $4,000 family..."));
        }

        if (question.Contains("copay", StringComparison.OrdinalIgnoreCase))
        {
            references.Add(new Reference(
                PlanDocumentId: "contoso-ppo-silver-2026",
                Anchor: "Copayments",
                Quote: "Primary care: $25, Specialist: $50, Emergency: $250..."));
        }

        // Always include at least one reference for grounded answers
        if (references.Count == 0)
        {
            references.Add(new Reference(
                PlanDocumentId: "contoso-ppo-silver-2026",
                Anchor: "Summary of Benefits",
                Quote: "This plan provides comprehensive medical coverage..."));
        }

        return references;
    }
}
