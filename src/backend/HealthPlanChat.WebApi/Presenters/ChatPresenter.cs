using HealthPlanChat.Core.UseCases.Chat;
using HealthPlanChat.Core.UseCases.Contracts;

namespace HealthPlanChat.WebApi.Presenters;

/// <summary>
/// Presenter for chat responses. Transforms use case output to API response.
/// </summary>
public sealed class ChatPresenter
{
    private readonly IChatInputBoundary _chatInteractor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatPresenter"/> class.
    /// </summary>
    /// <param name="chatInteractor">The chat interactor.</param>
    public ChatPresenter(IChatInputBoundary chatInteractor)
    {
        _chatInteractor = chatInteractor ?? throw new ArgumentNullException(nameof(chatInteractor));
    }

    /// <summary>
    /// Processes a chat message and returns the response.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat response.</returns>
    public async Task<ChatResponse> ProcessChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var input = new ChatInput(request.SessionId, request.Message);
        var output = await _chatInteractor.ExecuteAsync(input, cancellationToken);

        return new ChatResponse(
            output.AnswerType,
            output.AnswerText,
            output.References);
    }
}
