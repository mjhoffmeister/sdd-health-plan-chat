using HealthPlanChat.Core.UseCases.Contracts;
using HealthPlanChat.WebApi.Presenters;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

        group.MapPost("/sessions", CreateSession)
            .WithName("CreateSession")
            .WithSummary("Start a new chat session")
            .Produces<CreateSessionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/chat", SendMessage)
            .WithName("SendMessage")
            .WithSummary("Send a chat message")
            .Produces<ChatResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }

    /// <summary>
    /// Creates a new chat session.
    /// POST /api/sessions
    /// </summary>
    private static async Task<Results<Created<CreateSessionResponse>, StatusCodeHttpResult>> CreateSession(
        [FromServices] CreateSessionPresenter presenter,
        CancellationToken cancellationToken)
    {
        var response = await presenter.CreateSessionAsync(cancellationToken);
        return TypedResults.Created($"/api/sessions/{response.SessionId}", response);
    }

    /// <summary>
    /// Sends a chat message and receives a response.
    /// POST /api/chat
    /// </summary>
    private static async Task<Results<Ok<ChatResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> SendMessage(
        [FromBody] ChatRequest request,
        [FromServices] ChatPresenter presenter,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "SessionId is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "Message is required and cannot be empty.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var response = await presenter.ProcessChatAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found"))
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = "The specified session was not found or has expired.",
                Status = StatusCodes.Status404NotFound
            });
        }
    }
}
