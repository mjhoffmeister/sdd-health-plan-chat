using Microsoft.JSInterop;

namespace HealthPlanChat.Web.Services;

/// <summary>
/// Service for managing application theme (light/dark mode) with browser persistence.
/// </summary>
public sealed class ThemeService : IAsyncDisposable
{
    private const string StorageKey = "healthplanchat-theme";
    private const string LightTheme = "light";
    private const string DarkTheme = "dark";

    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = LightTheme;
    private bool _isInitialized;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    public string CurrentTheme => _currentTheme;

    /// <summary>
    /// Gets whether the current theme is dark mode.
    /// </summary>
    public bool IsDarkMode => _currentTheme == DarkTheme;

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    public event Action? OnThemeChanged;

    /// <summary>
    /// Initializes the theme service by loading the persisted theme from localStorage.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            var storedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);

            if (!string.IsNullOrEmpty(storedTheme) && (storedTheme == LightTheme || storedTheme == DarkTheme))
            {
                _currentTheme = storedTheme;
            }
            else
            {
                // Check system preference if no stored theme
                var prefersDark = await _jsRuntime.InvokeAsync<bool>("eval",
                    "window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches");
                _currentTheme = prefersDark ? DarkTheme : LightTheme;
            }

            await ApplyThemeToDocumentAsync();
            _isInitialized = true;
        }
        catch (JSException)
        {
            // Fallback to light theme if JS interop fails (e.g., during prerendering)
            _currentTheme = LightTheme;
        }
    }

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    public async Task ToggleThemeAsync()
    {
        _currentTheme = _currentTheme == LightTheme ? DarkTheme : LightTheme;

        await PersistThemeAsync();
        await ApplyThemeToDocumentAsync();

        OnThemeChanged?.Invoke();
    }

    /// <summary>
    /// Sets a specific theme.
    /// </summary>
    /// <param name="theme">The theme to set ("light" or "dark").</param>
    public async Task SetThemeAsync(string theme)
    {
        if (theme != LightTheme && theme != DarkTheme)
            return;

        if (_currentTheme == theme)
            return;

        _currentTheme = theme;

        await PersistThemeAsync();
        await ApplyThemeToDocumentAsync();

        OnThemeChanged?.Invoke();
    }

    private async Task PersistThemeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, _currentTheme);
        }
        catch (JSException)
        {
            // Ignore storage errors (e.g., localStorage disabled)
        }
    }

    private async Task ApplyThemeToDocumentAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval",
                $"document.documentElement.setAttribute('data-theme', '{_currentTheme}')");
        }
        catch (JSException)
        {
            // Ignore DOM manipulation errors during prerendering
        }
    }

    public ValueTask DisposeAsync()
    {
        // No resources to dispose
        return ValueTask.CompletedTask;
    }
}
