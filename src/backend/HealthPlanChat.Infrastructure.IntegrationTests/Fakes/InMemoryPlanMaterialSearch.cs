using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;

namespace HealthPlanChat.Infrastructure.IntegrationTests.Fakes;

/// <summary>
/// In-memory implementation of IPlanMaterialSearch for testing.
/// Simulates plan material search with keyword matching against seeded data.
/// </summary>
public sealed class InMemoryPlanMaterialSearch : IPlanMaterialSearch
{
    private readonly List<PlanMaterialChunk> _seedData;

    public InMemoryPlanMaterialSearch(IEnumerable<PlanMaterialChunk>? seedData = null)
    {
        _seedData = seedData?.ToList() ?? GetDefaultSeedData();
    }

    // Common English stop words that shouldn't contribute to relevance scoring
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "have",
        "he", "i", "in", "is", "it", "its", "my", "of", "on", "or", "she", "that", "the",
        "their", "there", "they", "this", "to", "was", "we", "what", "which", "with",
        "you", "your", "can", "do", "does", "how", "like", "much", "where", "when", "who"
    };

    // Health plan-specific keywords that indicate plan-related questions
    private static readonly HashSet<string> PlanKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "deductible", "deductibles", "copay", "copays", "copayment", "copayments",
        "premium", "premiums", "coverage", "covered", "network", "in-network", "out-of-network",
        "specialist", "referral", "referrals", "authorization", "prior", "prescription",
        "drug", "drugs", "medication", "medications", "generic", "brand", "pharmacy",
        "emergency", "urgent", "hospital", "hospitalization", "inpatient", "outpatient",
        "preventive", "preventative", "wellness", "annual", "maximum", "out-of-pocket",
        "coinsurance", "plan", "health", "medical", "doctor", "physician", "visit",
        "primary", "care", "pcp", "hmo", "ppo", "epo", "costs", "cost", "pay", "family",
        "individual", "bronze", "silver", "gold", "platinum"
    };

    public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        // Extract meaningful terms by filtering out stop words
        var queryTerms = query.ToLowerInvariant()
            .Split([' ', '?', '!', '.', ','], StringSplitOptions.RemoveEmptyEntries)
            .Where(term => !StopWords.Contains(term))
            .ToList();

        // If no meaningful terms, return empty results
        if (queryTerms.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<RetrievedChunk>>([]);
        }

        // Check if query contains any plan-related keywords
        var hasPlanKeyword = queryTerms.Any(term => PlanKeywords.Contains(term));

        // If no plan keywords at all, this is likely an out-of-scope question
        if (!hasPlanKeyword)
        {
            return Task.FromResult<IReadOnlyList<RetrievedChunk>>([]);
        }

        // For scoring, only use plan-related keywords to avoid dilution from filler words
        var scoringTerms = queryTerms.Where(t => PlanKeywords.Contains(t)).ToList();

        var results = _seedData
            .Select(chunk =>
            {
                // Calculate relevance score based on plan keyword matches
                var chunkText = $"{chunk.Section} {chunk.Text}".ToLowerInvariant();
                var matchCount = scoringTerms.Count(term => chunkText.Contains(term));

                // Score based on percentage of plan keywords matched
                // Requires at least 1 meaningful term match for any score
                var score = matchCount > 0 ? 0.5 + (0.5 * matchCount / Math.Max(scoringTerms.Count, 1)) : 0.0;

                return new
                {
                    Chunk = chunk,
                    Score = score
                };
            })
            .Where(x => x.Score >= 0.5) // Only return chunks with good relevance
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunk(
                ChunkId: x.Chunk.ChunkId,
                PlanDocumentId: x.Chunk.PlanDocumentId,
                PlanName: x.Chunk.PlanName,
                Section: x.Chunk.Section,
                Text: x.Chunk.Text,
                PageOrAnchor: x.Chunk.PageOrAnchor,
                Score: x.Score))
            .ToList();

        return Task.FromResult<IReadOnlyList<RetrievedChunk>>(results);
    }

    /// <summary>
    /// Default seed data representing plan materials.
    /// These simulate the content from data/plan-materials/*.json
    /// </summary>
    private static List<PlanMaterialChunk> GetDefaultSeedData() =>
    [
        new(
            ChunkId: "contoso-ppo-silver-deductible",
            PlanDocumentId: "contoso-ppo-silver-2026",
            PlanName: "Contoso Health PPO Silver 2026",
            Section: "Deductibles and Out-of-Pocket Costs",
            Text: "The annual deductible is $500 for individuals and $1,000 for families. " +
                  "The out-of-pocket maximum is $4,000 for individuals and $8,000 for families.",
            PageOrAnchor: "section-deductibles"),

        new(
            ChunkId: "contoso-ppo-silver-copay",
            PlanDocumentId: "contoso-ppo-silver-2026",
            PlanName: "Contoso Health PPO Silver 2026",
            Section: "Copayments and Copays",
            Text: "Your copays (copayments) for covered services are as follows: " +
                  "Primary care visits have a $25 copay. Specialist visits have a $50 copay. " +
                  "Emergency room visits have a $250 copay (waived if admitted).",
            PageOrAnchor: "section-copays"),

        new(
            ChunkId: "contoso-hmo-gold-deductible",
            PlanDocumentId: "contoso-hmo-gold-2026",
            PlanName: "Contoso Health HMO Gold 2026",
            Section: "Deductibles",
            Text: "There is no annual deductible for in-network services. " +
                  "Out-of-network services are not covered except for emergencies.",
            PageOrAnchor: "section-deductibles"),

        new(
            ChunkId: "contoso-hmo-gold-referrals",
            PlanDocumentId: "contoso-hmo-gold-2026",
            PlanName: "Contoso Health HMO Gold 2026",
            Section: "Referrals and Prior Authorization",
            Text: "A referral from your primary care physician is required before seeing a specialist. " +
                  "Prior authorization is required for hospital stays, MRI/CT scans, and certain procedures.",
            PageOrAnchor: "section-referrals"),

        new(
            ChunkId: "contoso-epo-bronze-deductible",
            PlanDocumentId: "contoso-epo-bronze-2026",
            PlanName: "Contoso Health EPO Bronze 2026",
            Section: "Deductibles",
            Text: "The annual deductible is $2,000 for individuals and $4,000 for families. " +
                  "Preventive care is covered at 100% before deductible.",
            PageOrAnchor: "section-deductibles"),

        new(
            ChunkId: "contoso-ppo-silver-prescription",
            PlanDocumentId: "contoso-ppo-silver-2026",
            PlanName: "Contoso Health PPO Silver 2026",
            Section: "Prescription Drug Coverage",
            Text: "Generic drugs have a $10 copay. Preferred brand drugs have a $35 copay. " +
                  "Non-preferred brand drugs have a $70 copay. Specialty drugs require 20% coinsurance.",
            PageOrAnchor: "section-prescription"),
    ];
}

/// <summary>
/// Represents a chunk of plan material for seeding the in-memory search.
/// </summary>
public sealed record PlanMaterialChunk(
    string ChunkId,
    string PlanDocumentId,
    string PlanName,
    string Section,
    string Text,
    string PageOrAnchor);
