using HealthPlanChat.Core.UseCases;
using HealthPlanChat.Core.UseCases.Chat;
using HealthPlanChat.WebApi.Contracts;
using Microsoft.AspNetCore.Mvc;
using CoreChatRequest = HealthPlanChat.Core.UseCases.Chat.ChatRequest;
using ApiChatRequest = HealthPlanChat.WebApi.Contracts.ChatRequest;
using ApiChatResponse = HealthPlanChat.WebApi.Contracts.ChatResponse;

namespace HealthPlanChat.WebApi.Endpoints;

/// <summary>
/// Maps chat API endpoints per OpenAPI specification.
/// </summary>
public static class ChatEndpoints
{
    /// <summary>
    /// Maps the chat endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The route group builder.</returns>
    public static RouteGroupBuilder MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Chat");

        group.MapPost("/chat", SendMessage)
            .WithName("SendMessage")
            .WithSummary("Send a chat message")
            .WithDescription("Sends a message to the health plan assistant. If no sessionId is provided, a new session is created.")
            .Produces<ApiChatResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }

    /// <summary>
    /// Sends a chat message and receives a response.
    /// POST /api/chat
    /// </summary>
    private static async Task<IResult> SendMessage(
        [FromBody] ApiChatRequest request,
        [FromServices] IUseCaseInteractor<CoreChatRequest, IResult> interactor,
        CancellationToken cancellationToken)
    {
        var coreRequest = new CoreChatRequest(request.SessionId, request.Message);
        return await interactor.HandleAsync(coreRequest, cancellationToken);
    }
}
