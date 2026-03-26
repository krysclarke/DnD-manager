using DnDManager.Models;

namespace DnDManager.Services;

public interface IThemeService {
    IReadOnlyList<AppTheme> AvailableThemes { get; }
    AppTheme CurrentTheme { get; }
    double CurrentScale { get; }
    event Action? ThemeChanged;
    event Action? ScaleChanged;
    void ApplyTheme(string themeId);
    void SetScale(double scale);
}
