using Azure.Identity;
using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Core.UseCases.Contracts;
using HealthPlanChat.Infrastructure.Prompting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthPlanChat.Infrastructure.AgentFramework;

/// <summary>
/// Agent Framework implementation of <see cref="IChatAgent"/> using Azure AI Foundry.
/// </summary>
public sealed class AgentFrameworkChatAgent : IChatAgent
{
    private readonly ILogger<AgentFrameworkChatAgent> _logger;
    private readonly FoundryOptions _options;
    private readonly PromptBuilder _promptBuilder;

    public AgentFrameworkChatAgent(
        IOptions<FoundryOptions> options,
        ILogger<AgentFrameworkChatAgent> logger)
    {
        _logger = logger;
        _options = options.Value;
        _promptBuilder = new PromptBuilder();
    }

    /// <inheritdoc />
    public async Task<ChatAgentResponse> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        IReadOnlyList<RetrievedChunk> retrievedChunks,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        _logger.LogInformation(
            "Generating response. HistoryCount: {HistoryCount}, ChunkCount: {ChunkCount}",
            history.Count,
            retrievedChunks.Count);

        try
        {
            // Build the system prompt with grounding context
            var systemPrompt = _promptBuilder.BuildSystemPrompt(retrievedChunks);
            var conversationHistory = _promptBuilder.BuildConversationHistory(history);

            // Call the chat completion API
            var responseText = await CallChatCompletionAsync(
                systemPrompt,
                conversationHistory,
                userMessage,
                cancellationToken);

            // Determine answer type and extract references
            var answerType = DetermineAnswerType(responseText, retrievedChunks);
            var references = ExtractReferences(responseText, retrievedChunks, answerType);

            _logger.LogInformation(
                "Response generated. AnswerType: {AnswerType}, ReferenceCount: {ReferenceCount}",
                answerType,
                references.Count);

            return new ChatAgentResponse(responseText, answerType, references);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to generate response. ExceptionType: {ExceptionType}",
                ex.GetType().Name);
            throw;
        }
    }

    private async Task<string> CallChatCompletionAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> history,
        string userMessage,
        CancellationToken cancellationToken)
    {
        // TODO: Implement actual Agent Framework call using Microsoft.Agents.AI
        // For now, return a placeholder that indicates the structure is in place
        // This will be fully implemented when the Agent Framework SDK patterns are finalized

        // Simulate API latency
        await Task.Delay(100, cancellationToken);

        // For foundation phase, return a structured placeholder
        // The actual implementation will use:
        // - Azure.AI.OpenAI or Microsoft.Agents.AI client
        // - DefaultAzureCredential for authentication
        // - Chat completion with the configured model

        _logger.LogWarning(
            "Using placeholder response. Endpoint: {Endpoint}, Model: {Model}",
            _options.Endpoint,
            _options.ChatModelDeployment);

        return "This is a placeholder response. The Agent Framework integration is pending full implementation. " +
               "Please ensure Azure AI Foundry is properly configured.";
    }

    private static AnswerType DetermineAnswerType(
        string responseText,
        IReadOnlyList<RetrievedChunk> retrievedChunks)
    {
        // If no chunks were retrieved, it's general guidance
        if (retrievedChunks.Count == 0)
        {
            return AnswerType.GeneralGuidance;
        }

        // Check if response indicates grounding
        // Look for citation patterns or references to plan documents
        foreach (var chunk in retrievedChunks)
        {
            if (responseText.Contains(chunk.PlanName, StringComparison.OrdinalIgnoreCase))
            {
                return AnswerType.Grounded;
            }
        }

        // If response mentions general guidance or inability to find info
        if (responseText.Contains("general guidance", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("cannot find", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("not found in", StringComparison.OrdinalIgnoreCase))
        {
            return AnswerType.GeneralGuidance;
        }

        // Default to grounded if chunks were available
        return AnswerType.Grounded;
    }

    private IReadOnlyList<Reference> ExtractReferences(
        string responseText,
        IReadOnlyList<RetrievedChunk> retrievedChunks,
        AnswerType answerType)
    {
        // No references for general guidance
        if (answerType == AnswerType.GeneralGuidance)
        {
            return Array.Empty<Reference>();
        }

        var extractedRefs = _promptBuilder.ExtractReferences(responseText, retrievedChunks);

        return extractedRefs
            .Select(r => new Reference(r.PlanDocumentId, r.Anchor, r.Quote))
            .ToList()
            .AsReadOnly();
    }
}
