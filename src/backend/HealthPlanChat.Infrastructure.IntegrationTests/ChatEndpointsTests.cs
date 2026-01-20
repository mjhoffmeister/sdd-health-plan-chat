using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HealthPlanChat.Core.Domain.Chat;
using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Infrastructure.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HealthPlanChat.Infrastructure.IntegrationTests;

/// <summary>
/// Integration tests for chat endpoints validating Phase 3 criteria.
/// Uses in-memory fakes with realistic behavior to test the full application pipeline.
///
/// Independent Test: "Calling /api/chat returns answerType=Grounded and non-empty
/// references for in-scope questions."
///
/// Note: After the agent-native RAG refactor, the agent handles retrieval internally
/// via Azure AI Search tool. IPlanMaterialSearch is no longer used by ChatInteractor.
/// Session management is handled automatically - if no sessionId is provided, a new
/// session is created and returned in the response.
/// </summary>
public class ChatEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates an HTTP client with in-memory fakes that have realistic behavior.
    /// The fakes simulate agent-native search behavior without external dependencies.
    /// </summary>
    private HttpClient CreateClientWithFakes()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove real implementations registered by Bootstrapper
                RemoveService<IChatSessionStore>(services);
                RemoveService<IChatAgent>(services);

                // Add in-memory fakes with realistic behavior
                // - InMemoryChatSessionStore: proper session lifecycle
                // - FakeChatAgent: simulates agent with Azure AI Search tool
                //   (analyzes user message to determine grounded vs general guidance)
                services.AddSingleton<IChatSessionStore>(new InMemoryChatSessionStore());
                services.AddSingleton<IChatAgent>(new FakeChatAgent());
            });
        }).CreateClient();
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);
    }

    /// <summary>
    /// Phase 3 Independent Test: In-scope question returns Grounded with references.
    ///
    /// This test proves the full pipeline works:
    /// 1. New session is created automatically (no sessionId provided)
    /// 2. User message triggers search against seeded plan materials
    /// 3. Relevant chunks are found (deductible info)
    /// 4. Agent generates grounded response with references
    /// 5. Response includes sessionId, answerType=Grounded and non-empty references
    /// </summary>
    [Fact]
    public async Task InScopeQuestion_ReturnsGroundedAnswerWithReferences()
    {
        // Arrange
        var client = CreateClientWithFakes();

        // Act: Ask an in-scope question about deductibles (no sessionId = new session)
        var chatRequest = new ChatRequest(
            SessionId: null,
            Message: "What is my deductible?");

        var chatResponse = await client.PostAsJsonAsync("/api/chat", chatRequest);

        // Assert: Phase 3 Independent Test criteria
        chatResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await chatResponse.Content.ReadFromJsonAsync<ChatResponse>();
        response.Should().NotBeNull();

        // Session should be created and returned
        response!.SessionId.Should().NotBeNullOrWhiteSpace(
            because: "a new session should be created when none is provided");

        // Core assertion: answerType must be Grounded for in-scope questions
        response.AnswerType.Should().Be(AnswerType.Grounded,
            because: "questions about deductibles match seeded plan materials");

        // Core assertion: references must not be empty
        response.References.Should().NotBeEmpty(
            because: "grounded answers must cite their sources");

        // Verify references have proper structure
        response.References.Should().AllSatisfy(r =>
        {
            r.PlanDocumentId.Should().NotBeNullOrWhiteSpace();
            r.Anchor.Should().NotBeNullOrWhiteSpace();
            r.Quote.Should().NotBeNullOrWhiteSpace();
        });

        // Verify answer text is substantive
        response.AnswerText.Should().NotBeNullOrWhiteSpace();
        response.AnswerText.Should().Contain("deductible",
            because: "answer should address the user's question");
    }

    /// <summary>
    /// Out-of-scope question returns GeneralGuidance with no references.
    /// This proves the system correctly handles questions not covered by plan materials.
    /// </summary>
    [Fact]
    public async Task OutOfScopeQuestion_ReturnsGeneralGuidanceWithNoReferences()
    {
        // Arrange
        var client = CreateClientWithFakes();

        // Act: Ask a question that won't match any seeded plan material
        var chatRequest = new ChatRequest(
            SessionId: null,
            Message: "What is the weather like on Mars?");

        var chatResponse = await client.PostAsJsonAsync("/api/chat", chatRequest);

        // Assert
        chatResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await chatResponse.Content.ReadFromJsonAsync<ChatResponse>();
        response.Should().NotBeNull();

        // Session should be created
        response!.SessionId.Should().NotBeNullOrWhiteSpace();

        // Out-of-scope questions should return GeneralGuidance
        response.AnswerType.Should().Be(AnswerType.GeneralGuidance,
            because: "questions not matching plan materials should be general guidance");

        // GeneralGuidance should have empty references
        response.References.Should().BeEmpty(
            because: "general guidance has no plan document citations");
    }

    /// <summary>
    /// Validates POST /api/chat returns 400 when message is empty.
    /// </summary>
    [Fact]
    public async Task PostChat_WithEmptyMessage_Returns400BadRequest()
    {
        // Arrange
        var client = CreateClientWithFakes();
        var request = new ChatRequest(SessionId: null, Message: "");

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Validates POST /api/chat returns 404 when session doesn't exist.
    /// </summary>
    [Fact]
    public async Task PostChat_WithInvalidSession_Returns404NotFound()
    {
        // Arrange
        var client = CreateClientWithFakes();
        var request = new ChatRequest(
            SessionId: "non-existent-session-id",
            Message: "What is my deductible?");

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Validates GET /healthz returns 200 OK.
    /// </summary>
    [Fact]
    public async Task GetHealthz_Returns200Ok()
    {
        // Arrange
        var client = CreateClientWithFakes();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Multi-turn conversation maintains session context.
    /// </summary>
    [Fact]
    public async Task MultiTurnConversation_MaintainsSessionContext()
    {
        // Arrange
        var client = CreateClientWithFakes();

        // Act: First message (creates session)
        var request1 = new ChatRequest(SessionId: null, Message: "What is my deductible?");
        var response1 = await client.PostAsJsonAsync("/api/chat", request1);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var chat1 = await response1.Content.ReadFromJsonAsync<ChatResponse>();
        chat1.Should().NotBeNull();
        chat1!.SessionId.Should().NotBeNullOrWhiteSpace();

        // Act: Second message using returned sessionId
        var request2 = new ChatRequest(SessionId: chat1.SessionId, Message: "What about copays?");
        var response2 = await client.PostAsJsonAsync("/api/chat", request2);

        // Assert: Both should succeed
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var chat2 = await response2.Content.ReadFromJsonAsync<ChatResponse>();
        chat2.Should().NotBeNull();

        // Same session should be used
        chat2!.SessionId.Should().Be(chat1.SessionId);

        // Both should be grounded (both topics are in seeded data)
        chat1.AnswerType.Should().Be(AnswerType.Grounded);
        chat2.AnswerType.Should().Be(AnswerType.Grounded);
    }
}

// DTOs for deserialization (matching API contracts)
public record ChatRequest(string? SessionId, string Message);
public record ChatResponse(string SessionId, AnswerType AnswerType, string AnswerText, IReadOnlyList<Reference> References);
