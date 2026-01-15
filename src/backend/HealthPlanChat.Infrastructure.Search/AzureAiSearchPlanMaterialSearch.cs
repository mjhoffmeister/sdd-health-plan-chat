using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthPlanChat.Infrastructure.Search;

/// <summary>
/// Azure AI Search implementation of <see cref="IPlanMaterialSearch"/>.
/// </summary>
public sealed class AzureAiSearchPlanMaterialSearch : IPlanMaterialSearch
{
    private readonly ILogger<AzureAiSearchPlanMaterialSearch> _logger;
    private readonly SearchOptions _options;
    private readonly Lazy<SearchClient> _searchClient;

    public AzureAiSearchPlanMaterialSearch(
        IOptions<SearchOptions> options,
        ILogger<AzureAiSearchPlanMaterialSearch> logger)
    {
        _logger = logger;
        _options = options.Value;

        _searchClient = new Lazy<SearchClient>(() =>
        {
            var endpoint = new Uri(_options.Endpoint);
            var credential = new DefaultAzureCredential();
            return new SearchClient(endpoint, _options.IndexName, credential);
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        _logger.LogInformation(
            "Searching plan materials. TopK: {TopK}",
            topK);

        try
        {
            var searchOptions = new Azure.Search.Documents.SearchOptions
            {
                Size = topK,
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "default-semantic",
                    QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
                    QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive)
                },
                Select =
                {
                    "id",
                    "chunkId",
                    "planDocumentId",
                    "planName",
                    "section",
                    "text",
                    "pageOrAnchor"
                }
            };

            var response = await _searchClient.Value.SearchAsync<SearchDocument>(
                query,
                searchOptions,
                cancellationToken);

            var chunks = new List<RetrievedChunk>();

            await foreach (var result in response.Value.GetResultsAsync())
            {
                var doc = result.Document;
                var score = result.Score ?? 0;

                var chunk = new RetrievedChunk(
                    ChunkId: GetStringValue(doc, "chunkId") ?? GetStringValue(doc, "id") ?? string.Empty,
                    PlanDocumentId: GetStringValue(doc, "planDocumentId") ?? string.Empty,
                    PlanName: GetStringValue(doc, "planName") ?? string.Empty,
                    Section: GetStringValue(doc, "section") ?? string.Empty,
                    Text: GetStringValue(doc, "text") ?? string.Empty,
                    PageOrAnchor: GetStringValue(doc, "pageOrAnchor") ?? string.Empty,
                    Score: score);

                chunks.Add(chunk);
            }

            _logger.LogInformation(
                "Search completed. ResultCount: {ResultCount}",
                chunks.Count);

            return chunks.AsReadOnly();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                "Search request failed. Status: {Status}",
                ex.Status);
            throw;
        }
    }

    private static string? GetStringValue(SearchDocument doc, string key)
    {
        return doc.TryGetValue(key, out var value) ? value?.ToString() : null;
    }
}
