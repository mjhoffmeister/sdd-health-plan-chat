using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Core.UseCases.Contracts;

namespace HealthPlanChat.WebApi.Presenters;

/// <summary>
/// Presenter for session creation. Thin wrapper over IChatSessionStore.CreateSession.
/// </summary>
public sealed class CreateSessionPresenter
{
    private readonly IChatSessionStore _sessionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSessionPresenter"/> class.
    /// </summary>
    /// <param name="sessionStore">The session store.</param>
    public CreateSessionPresenter(IChatSessionStore sessionStore)
    {
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session creation response.</returns>
    public async Task<CreateSessionResponse> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await _sessionStore.CreateSessionAsync(cancellationToken);
        return new CreateSessionResponse(session.ChatSessionId);
    }
}
