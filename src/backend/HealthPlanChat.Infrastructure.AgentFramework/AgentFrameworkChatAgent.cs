using System.Text.RegularExpressions;
using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Infrastructure.Prompting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthPlanChat.Infrastructure.AgentFramework;

/// <summary>
/// Agent Framework implementation of <see cref="IChatAgent"/> using Azure AI Foundry.
/// Uses AzureAISearchAgentTool for native RAG - agent handles retrieval internally.
/// </summary>
public sealed class AgentFrameworkChatAgent : IChatAgent
{
    private readonly ILogger<AgentFrameworkChatAgent> _logger;
    private readonly FoundryOptions _options;
    private readonly PromptBuilder _promptBuilder;
    private readonly AIProjectClient _projectClient;

    public AgentFrameworkChatAgent(
        IOptions<FoundryOptions> options,
        ILogger<AgentFrameworkChatAgent> logger)
    {
        _logger = logger;
        _options = options.Value;
        _promptBuilder = new PromptBuilder();

        // Initialize the AI Project client for Azure AI Foundry
        _projectClient = new AIProjectClient(
            new Uri(_options.Endpoint),
            new DefaultAzureCredential());
    }

    /// <inheritdoc />
    public async Task<ChatAgentResponse> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        _logger.LogInformation(
            "Generating response with agent-native search. HistoryCount: {HistoryCount}",
            history.Count);

        try
        {
            // Build the system prompt (agent will use search tool)
            var systemPrompt = _promptBuilder.BuildSystemPrompt();
            var conversationHistory = _promptBuilder.BuildConversationHistory(history);

            // Get the persistent agents client
            var agentsClient = _projectClient.GetPersistentAgentsClient();

            // Configure the Azure AI Search tool resource
            var searchResource = new AzureAISearchToolResource(
                _options.SearchConnectionId,
                _options.SearchIndexName,
                _options.SearchTopK,
                filter: null,
                AzureAISearchQueryType.VectorSemanticHybrid);

            var toolResources = new ToolResources
            {
                AzureAISearch = searchResource
            };

            // Create the agent definition with search tool
            PersistentAgent agent = await agentsClient.Administration.CreateAgentAsync(
                model: _options.ChatModelDeployment,
                name: "HealthPlanAssistant",
                instructions: systemPrompt,
                tools: [new AzureAISearchToolDefinition()],
                toolResources: toolResources,
                cancellationToken: cancellationToken);

            try
            {
                // Create a thread for the conversation
                PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync(
                    cancellationToken: cancellationToken);

                try
                {
                    // Add conversation history
                    foreach (var (role, content) in conversationHistory)
                    {
                        var messageRole = role == "user" ? MessageRole.User : MessageRole.Agent;
                        await agentsClient.Messages.CreateMessageAsync(
                            thread.Id,
                            messageRole,
                            content,
                            cancellationToken: cancellationToken);
                    }

                    // Add the current user message
                    await agentsClient.Messages.CreateMessageAsync(
                        thread.Id,
                        MessageRole.User,
                        userMessage,
                        cancellationToken: cancellationToken);

                    // Run the agent
                    ThreadRun run = await agentsClient.Runs.CreateRunAsync(
                        thread.Id,
                        agent.Id,
                        cancellationToken: cancellationToken);

                    // Wait for completion
                    while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
                    {
                        await Task.Delay(500, cancellationToken);
                        run = await agentsClient.Runs.GetRunAsync(
                            thread.Id,
                            run.Id,
                            cancellationToken: cancellationToken);
                    }

                    if (run.Status != RunStatus.Completed)
                    {
                        _logger.LogError(
                            "Agent run failed. Status: {Status}, Error: {Error}",
                            run.Status,
                            run.LastError?.Message ?? "Unknown");
                        throw new InvalidOperationException($"Agent run failed with status: {run.Status}");
                    }

                    // Get the messages (response is the latest assistant message)
                    var messages = agentsClient.Messages.GetMessagesAsync(
                        thread.Id,
                        cancellationToken: cancellationToken);

                    PersistentThreadMessage? assistantMessage = null;
                    await foreach (var message in messages)
                    {
                        if (message.Role == MessageRole.Agent)
                        {
                            assistantMessage = message;
                            break;
                        }
                    }

                    if (assistantMessage == null)
                    {
                        throw new InvalidOperationException("No assistant response found");
                    }

                    // Extract text content and citations
                    var (responseText, references) = ExtractResponseAndCitations(assistantMessage);

                    // Determine answer type from response text (before sanitization)
                    var answerType = DetermineAnswerType(responseText, references);

                    // Sanitize response text: strip answer type labels and citation markers
                    var sanitizedText = ResponseTextSanitizer.Sanitize(responseText);

                    _logger.LogInformation(
                        "Response generated. AnswerType: {AnswerType}, ReferenceCount: {ReferenceCount}",
                        answerType,
                        references.Count);

                    return new ChatAgentResponse(sanitizedText, answerType, references);
                }
                finally
                {
                    // Clean up thread
                    await agentsClient.Threads.DeleteThreadAsync(thread.Id, cancellationToken);
                }
            }
            finally
            {
                // Clean up agent
                await agentsClient.Administration.DeleteAgentAsync(agent.Id, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to generate response. ExceptionType: {ExceptionType}",
                ex.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Extracts response text and citation references from agent message.
    /// </summary>
    private (string ResponseText, IReadOnlyList<Reference> References) ExtractResponseAndCitations(
        PersistentThreadMessage message)
    {
        var references = new List<Reference>();
        var responseText = string.Empty;

        foreach (var contentItem in message.ContentItems)
        {
            if (contentItem is MessageTextContent textContent)
            {
                responseText = textContent.Text;

                // Extract citations from annotations
                foreach (var annotation in textContent.Annotations)
                {
                    if (annotation is MessageTextUriCitationAnnotation uriCitation)
                    {
                        // Parse citation into Reference (from AI Search grounding)
                        var reference = new Reference(
                            PlanDocumentId: ExtractDocumentIdFromUrl(uriCitation.UriCitation.Uri),
                            Anchor: uriCitation.UriCitation.Title ?? uriCitation.Text,
                            Quote: uriCitation.Text);

                        // Avoid duplicates
                        if (!references.Any(r => r.PlanDocumentId == reference.PlanDocumentId && r.Anchor == reference.Anchor))
                        {
                            references.Add(reference);
                        }
                    }
                    else if (annotation is MessageTextFileCitationAnnotation fileCitation)
                    {
                        // Parse file citation into Reference (from file_search tool)
                        var reference = new Reference(
                            PlanDocumentId: fileCitation.FileId ?? fileCitation.Text,
                            Anchor: fileCitation.Text,
                            Quote: fileCitation.Quote ?? fileCitation.Text);

                        // Avoid duplicates
                        if (!references.Any(r => r.PlanDocumentId == reference.PlanDocumentId && r.Anchor == reference.Anchor))
                        {
                            references.Add(reference);
                        }
                    }
                }
            }
        }

        return (responseText, references.AsReadOnly());
    }

    /// <summary>
    /// Extracts document ID from citation URL.
    /// </summary>
    private static string ExtractDocumentIdFromUrl(string url)
    {
        // URLs from AI Search typically contain the document key
        // Example: https://storage.blob.core.windows.net/plan-materials/contoso-hmo-gold-2026.json
        // Extract the filename without extension as the document ID
        try
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
            return !string.IsNullOrEmpty(fileName) ? fileName : url;
        }
        catch
        {
            return url;
        }
    }

    /// <summary>
    /// Determines answer type based on response content and citations.
    /// </summary>
    private static AnswerType DetermineAnswerType(string responseText, IReadOnlyList<Reference> references)
    {
        // If response explicitly starts with [GROUNDED] or [GENERAL GUIDANCE], use that
        if (responseText.Contains("[GROUNDED]", StringComparison.OrdinalIgnoreCase))
        {
            return AnswerType.Grounded;
        }

        if (responseText.Contains("[GENERAL GUIDANCE]", StringComparison.OrdinalIgnoreCase))
        {
            return AnswerType.GeneralGuidance;
        }

        // Fallback: if we have citations, consider it grounded
        return references.Count > 0 ? AnswerType.Grounded : AnswerType.GeneralGuidance;
    }
}
