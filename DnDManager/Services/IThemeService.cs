using DnDManager.Models;

namespace DnDManager.Services;

public interface IThemeService {
    IReadOnlyList<AppTheme> AvailableThemes { get; }
    AppTheme CurrentTheme { get; }
    double CurrentScale { get; }
    event Action? ThemeChanged;
    event Action? ScaleChanged;
    void ApplyTheme(string themeId);
    void ApplyThemeLive(AppTheme theme);
    void SetScale(double scale);
    Task LoadCustomThemesAsync(ICampaignRepository repository);
    Task SaveCustomThemesAsync(ICampaignRepository repository);
    void AddCustomTheme(AppTheme theme);
    void UpdateCustomTheme(AppTheme theme);
    void DeleteCustomTheme(string themeId);
}
