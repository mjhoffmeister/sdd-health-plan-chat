using HealthPlanChat.Core.UseCases.Chat;
using ApiChatResponse = HealthPlanChat.WebApi.Contracts.ChatResponse;
using CoreChatResponse = HealthPlanChat.Core.UseCases.Chat.ChatResponse;

namespace HealthPlanChat.WebApi.Presenters;

/// <summary>
/// Presenter for chat responses. Implements the boundary interface to transform
/// use case outcomes into API responses (IResult).
/// </summary>
public sealed class ChatPresenter : IChatBoundary<IResult>
{
    /// <inheritdoc />
    public IResult ChatCompleted(CoreChatResponse response)
    {
        var apiResponse = new ApiChatResponse(
            response.SessionId,
            response.AnswerType,
            response.AnswerText,
            response.References);

        return Results.Ok(apiResponse);
    }

    /// <inheritdoc />
    public IResult SessionNotFound(string sessionId)
    {
        return Results.NotFound(new
        {
            Title = "Not Found",
            Detail = "The specified session was not found or has expired.",
            Status = StatusCodes.Status404NotFound
        });
    }

    /// <inheritdoc />
    public IResult ValidationFailed(string errorMessage)
    {
        return Results.BadRequest(new
        {
            Title = "Bad Request",
            Detail = errorMessage,
            Status = StatusCodes.Status400BadRequest
        });
    }
}
