namespace HealthPlanChat.Web.Services;

/// <summary>
/// Service for managing chat session state.
/// </summary>
public sealed class ChatSessionService
{
    private readonly ApiClient _apiClient;
    private string? _sessionId;
    private readonly List<ChatMessageViewModel> _messages = [];
    private bool _isLoading;
    private string? _error;

    public ChatSessionService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets the current session ID.
    /// </summary>
    public string? SessionId => _sessionId;

    /// <summary>
    /// Gets the chat messages.
    /// </summary>
    public IReadOnlyList<ChatMessageViewModel> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Gets whether a request is in progress.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? Error => _error;

    /// <summary>
    /// Gets whether a session has been initialized.
    /// </summary>
    public bool HasSession => !string.IsNullOrEmpty(_sessionId);

    /// <summary>
    /// Event raised when state changes.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Initializes a new chat session (clears state, session ID assigned on first message).
    /// </summary>
    public Task InitializeSessionAsync(CancellationToken cancellationToken = default)
    {
        // Session is created implicitly on first message, just mark as ready
        _sessionId = string.Empty; // Empty string indicates ready but no server-side session yet
        _messages.Clear();
        _error = null;
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to the chat.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        try
        {
            _isLoading = true;
            _error = null;

            // Add user message immediately
            _messages.Add(new ChatMessageViewModel
            {
                Role = MessageRole.User,
                Text = message,
                Timestamp = DateTime.Now
            });
            NotifyStateChanged();

            // Send to API (null sessionId for first message creates a new session)
            var sessionIdToSend = string.IsNullOrEmpty(_sessionId) ? null : _sessionId;
            var request = new ChatRequest(sessionIdToSend, message);
            var response = await _apiClient.SendMessageAsync(request, cancellationToken);

            if (response is not null)
            {
                // Store the session ID from response (important for first message)
                _sessionId = response.SessionId;

                // Add assistant message
                _messages.Add(new ChatMessageViewModel
                {
                    Role = MessageRole.Assistant,
                    Text = response.AnswerText,
                    AnswerType = response.AnswerType,
                    References = response.References.ToList(),
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                _error = "Failed to get response. Please try again.";
            }
        }
        catch (HttpRequestException ex)
        {
            _error = $"Network error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _error = $"An error occurred: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Clears the current chat and starts a new session.
    /// </summary>
    public async Task NewChatAsync(CancellationToken cancellationToken = default)
    {
        _messages.Clear();
        _sessionId = null;
        _error = null;
        NotifyStateChanged();

        await InitializeSessionAsync(cancellationToken);
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}

/// <summary>
/// View model for a chat message.
/// </summary>
public sealed class ChatMessageViewModel
{
    public MessageRole Role { get; init; }
    public string Text { get; init; } = string.Empty;
    public AnswerType? AnswerType { get; init; }
    public List<Reference> References { get; init; } = [];
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// The role of the message sender.
/// </summary>
public enum MessageRole
{
    User,
    Assistant
}
