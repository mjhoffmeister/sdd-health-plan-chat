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
    /// Initializes a new chat session.
    /// </summary>
    public async Task InitializeSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _isLoading = true;
            _error = null;
            NotifyStateChanged();

            var response = await _apiClient.CreateSessionAsync(cancellationToken);
            if (response is not null)
            {
                _sessionId = response.SessionId;
                _messages.Clear();
            }
            else
            {
                _error = "Failed to create session. Please try again.";
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
    /// Sends a message to the chat.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (string.IsNullOrEmpty(_sessionId))
        {
            _error = "No active session. Please start a new chat.";
            NotifyStateChanged();
            return;
        }

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

            // Send to API
            var request = new ChatRequest(_sessionId, message);
            var response = await _apiClient.SendMessageAsync(request, cancellationToken);

            if (response is not null)
            {
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
