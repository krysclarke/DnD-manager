using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class SettingsViewModel : ObservableObject {
    private readonly IThemeService _themeService;
    private readonly ICampaignRepository _campaignRepository;
    private bool _isLoading;

    public IThemeService ThemeService => _themeService;
    public IReadOnlyList<AppTheme> AvailableThemes => _themeService.AvailableThemes;

    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private double _uiScale = 1.0;

    [ObservableProperty]
    private double _webUiScale = 1.0;

    [ObservableProperty]
    private AppTheme _selectedWebTheme;

    [ObservableProperty]
    private string? _contrastWarning;

    public string UiScaleDisplay => $"{UiScale:0.00}x";
    public string WebUiScaleDisplay => $"{WebUiScale:0.00}x";

    public WebServerViewModel? WebServerVm { get; set; }

    public event Action? WebThemeChanged;
    public event Action? WebUiScaleChanged;

    public SettingsViewModel(IThemeService themeService, ICampaignRepository campaignRepository) {
        _themeService = themeService;
        _campaignRepository = campaignRepository;
        _selectedTheme = _themeService.CurrentTheme;
        _selectedWebTheme = _themeService.CurrentTheme;
    }

    partial void OnSelectedThemeChanged(AppTheme value) {
        _themeService.ApplyTheme(value.Id);
        if (!_isLoading) {
            _ = _campaignRepository.SaveSettingAsync("theme", value.Id);
        }
        CheckContrast();
    }

    partial void OnUiScaleChanged(double value) {
        _themeService.SetScale(value);
        OnPropertyChanged(nameof(UiScaleDisplay));
        if (!_isLoading) {
            _ = _campaignRepository.SaveSettingAsync("uiScale", value.ToString("F2"));
        }
    }

    partial void OnWebUiScaleChanged(double value) {
        OnPropertyChanged(nameof(WebUiScaleDisplay));
        if (!_isLoading) {
            _ = _campaignRepository.SaveSettingAsync("webUiScale", value.ToString("F2"));
        }
        WebUiScaleChanged?.Invoke();
    }

    partial void OnSelectedWebThemeChanged(AppTheme value) {
        if (!_isLoading) {
            _ = _campaignRepository.SaveSettingAsync("webTheme", value.Id);
        }
        WebThemeChanged?.Invoke();
    }

    [RelayCommand]
    private void IncreaseScale() {
        UiScale = Math.Min(2.0, Math.Round((UiScale + 0.25) * 4) / 4);
    }

    [RelayCommand]
    private void DecreaseScale() {
        UiScale = Math.Max(0.5, Math.Round((UiScale - 0.25) * 4) / 4);
    }

    [RelayCommand]
    private void IncreaseWebScale() {
        WebUiScale = Math.Min(2.0, Math.Round((WebUiScale + 0.25) * 4) / 4);
    }

    [RelayCommand]
    private void DecreaseWebScale() {
        WebUiScale = Math.Max(0.5, Math.Round((WebUiScale - 0.25) * 4) / 4);
    }

    public void LoadSettings(string? themeId, double uiScale, double webUiScale, string? webThemeId = null) {
        _isLoading = true;
        try {
            UiScale = Math.Clamp(uiScale, 0.5, 2.0);
            WebUiScale = Math.Clamp(webUiScale, 0.5, 2.0);

            var theme = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == themeId)
                        ?? _themeService.AvailableThemes[0];

            SelectedTheme = theme;
            _themeService.ApplyTheme(theme.Id);

            var webTheme = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == webThemeId)
                           ?? theme;
            SelectedWebTheme = webTheme;
        } finally {
            _isLoading = false;
        }

        CheckContrast();
    }

    private void CheckContrast() {
        var theme = _themeService.CurrentTheme;
        // Check primary text contrast: AccentForeground on Accent
        var result = WcagContrastChecker.Check(theme.AccentForeground, theme.Accent);
        if (!result.MeetsAA) {
            ContrastWarning = $"Warning: Accent text contrast ratio is {result.Ratio:F1}:1 (below WCAG AA 4.5:1)";
            return;
        }

        // Check code text on code background
        var codeResult = WcagContrastChecker.Check(theme.CodeText, theme.CodeBlockBg);
        if (!codeResult.MeetsAA) {
            ContrastWarning = $"Warning: Code text contrast ratio is {codeResult.Ratio:F1}:1 (below WCAG AA 4.5:1)";
            return;
        }

        ContrastWarning = null;
    }
}
