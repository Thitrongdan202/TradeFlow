namespace TradeFlow.Web.Services;

public interface IThemeService
{
    string CurrentTheme { get; }
    bool IsDark { get; }
    event Action? OnThemeChanged;
    void SetTheme(string theme);
}
