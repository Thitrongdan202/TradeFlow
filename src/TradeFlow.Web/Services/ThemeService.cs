namespace TradeFlow.Web.Services;

public class ThemeService : IThemeService
{
    private string _currentTheme = "light";

    public string CurrentTheme => _currentTheme;
    public bool IsDark => _currentTheme == "dark";
    public event Action? OnThemeChanged;

    public void SetTheme(string theme)
    {
        var normalized = theme?.ToLowerInvariant() == "dark" ? "dark" : "light";
        if (_currentTheme != normalized)
        {
            _currentTheme = normalized;
            OnThemeChanged?.Invoke();
        }
    }
}
