using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using DnDManager.Models;

namespace DnDManager.Services;

public class ThemeService : IThemeService {
    private const string SystemThemeId = "system";
    private ResourceDictionary? _themeResources;
    private bool _subscribedToSystemTheme;

    public IReadOnlyList<AppTheme> AvailableThemes { get; } = new List<AppTheme> {
        new() {
            Id = SystemThemeId,
            DisplayName = "System",
            BaseVariant = ThemeVariant.Default,
            // Placeholder colors — actual colors are resolved dynamically based on OS theme
            CodeBlockBg = Color.Parse("#F0F0F0"),
            InlineCodeBg = Color.Parse("#E8E8E8"),
            CodeText = Color.Parse("#1E1E1E"),
            BlockquoteBorder = Color.Parse("#CCCCCC"),
            LinkColor = Color.Parse("#005FB8"),
            TableBorder = Color.Parse("#CCCCCC"),
            MutedText = Color.Parse("#666666"),
            RuleBrush = Color.Parse("#CCCCCC"),
            Nat1Brush = Color.Parse("#C42B1C"),
            Nat20Brush = Color.Parse("#0F7B0F"),
            Nat1Bg = Colors.Transparent,
            Nat20Bg = Colors.Transparent,
            OverlayBg = Color.Parse("#80000000"),
            DialogBg = Color.Parse("#FFF3F3F3"),
            ActiveHighlight = Color.Parse("#40005FB8"),
            HoverHighlight = Color.Parse("#20000000"),
            Surface = Color.Parse("#F3F3F3"),
            Accent = Color.Parse("#005FB8"),
            AccentForeground = Color.Parse("#FFFFFF")
        },
        new() {
            Id = "parchment",
            DisplayName = "Parchment",
            BaseVariant = ThemeVariant.Light,
            CodeBlockBg = Color.Parse("#E8D5B0"),
            InlineCodeBg = Color.Parse("#DFC9A0"),
            CodeText = Color.Parse("#3B2D1F"),
            BlockquoteBorder = Color.Parse("#A0845C"),
            LinkColor = Color.Parse("#6B4226"),
            TableBorder = Color.Parse("#C4A97D"),
            MutedText = Color.Parse("#7A6548"),
            RuleBrush = Color.Parse("#A0845C"),
            Nat1Brush = Color.Parse("#FFFFFF"),
            Nat20Brush = Color.Parse("#FFFFFF"),
            Nat1Bg = Color.Parse("#B71C1C"),
            Nat20Bg = Color.Parse("#2E7D32"),
            OverlayBg = Color.Parse("#80000000"),
            DialogBg = Color.Parse("#FFF5E6C8"),
            ActiveHighlight = Color.Parse("#40DAA520"),
            HoverHighlight = Color.Parse("#20000000"),
            Surface = Color.Parse("#F0DCC0"),
            Accent = Color.Parse("#8B4513"),
            AccentForeground = Color.Parse("#FFFFFF")
        },
        new() {
            Id = "high-contrast-light",
            DisplayName = "High-Contrast Light",
            BaseVariant = ThemeVariant.Light,
            CodeBlockBg = Color.Parse("#E0E0E0"),
            InlineCodeBg = Color.Parse("#D6D6D6"),
            CodeText = Color.Parse("#1A1A1A"),
            BlockquoteBorder = Color.Parse("#555555"),
            LinkColor = Color.Parse("#0044CC"),
            TableBorder = Color.Parse("#333333"),
            MutedText = Color.Parse("#444444"),
            RuleBrush = Color.Parse("#555555"),
            Nat1Brush = Color.Parse("#FFFFFF"),
            Nat20Brush = Color.Parse("#FFFFFF"),
            Nat1Bg = Color.Parse("#B71C1C"),
            Nat20Bg = Color.Parse("#1B5E20"),
            OverlayBg = Color.Parse("#80000000"),
            DialogBg = Color.Parse("#FFF5F5F5"),
            ActiveHighlight = Color.Parse("#400044CC"),
            HoverHighlight = Color.Parse("#20000000"),
            Surface = Color.Parse("#F5F5F5"),
            Accent = Color.Parse("#0044CC"),
            AccentForeground = Color.Parse("#FFFFFF")
        },
        new() {
            Id = "high-contrast-dark",
            DisplayName = "High-Contrast Dark",
            BaseVariant = ThemeVariant.Dark,
            CodeBlockBg = Color.Parse("#1A1A1A"),
            InlineCodeBg = Color.Parse("#2A2A2A"),
            CodeText = Color.Parse("#F0F0F0"),
            BlockquoteBorder = Color.Parse("#666666"),
            LinkColor = Color.Parse("#6CB2EB"),
            TableBorder = Color.Parse("#555555"),
            MutedText = Color.Parse("#AAAAAA"),
            RuleBrush = Color.Parse("#666666"),
            Nat1Brush = Color.Parse("#FF4444"),
            Nat20Brush = Color.Parse("#44DD44"),
            Nat1Bg = Colors.Transparent,
            Nat20Bg = Colors.Transparent,
            OverlayBg = Color.Parse("#CC000000"),
            DialogBg = Color.Parse("#FF1E1E2E"),
            ActiveHighlight = Color.Parse("#40FFD700"),
            HoverHighlight = Color.Parse("#30FFFFFF"),
            Surface = Color.Parse("#1E1E1E"),
            Accent = Color.Parse("#2D6CA3"),
            AccentForeground = Color.Parse("#FFFFFF")
        },
        new() {
            Id = "arcane",
            DisplayName = "Arcane",
            BaseVariant = ThemeVariant.Dark,
            CodeBlockBg = Color.Parse("#0D1B2A"),
            InlineCodeBg = Color.Parse("#152238"),
            CodeText = Color.Parse("#C8E0F4"),
            BlockquoteBorder = Color.Parse("#1B6B93"),
            LinkColor = Color.Parse("#4FC3F7"),
            TableBorder = Color.Parse("#1B4B6B"),
            MutedText = Color.Parse("#8BADC4"),
            RuleBrush = Color.Parse("#1B6B93"),
            Nat1Brush = Color.Parse("#FF5252"),
            Nat20Brush = Color.Parse("#40E0D0"),
            Nat1Bg = Colors.Transparent,
            Nat20Bg = Colors.Transparent,
            OverlayBg = Color.Parse("#CC050E1A"),
            DialogBg = Color.Parse("#FF0D1B2A"),
            ActiveHighlight = Color.Parse("#4000BCD4"),
            HoverHighlight = Color.Parse("#20C8E0F4"),
            Surface = Color.Parse("#112233"),
            Accent = Color.Parse("#008093"),
            AccentForeground = Color.Parse("#FFFFFF")
        },
        new() {
            Id = "purple",
            DisplayName = "Purple",
            BaseVariant = ThemeVariant.Dark,
            CodeBlockBg = Color.Parse("#1D1433"),
            InlineCodeBg = Color.Parse("#2A1F45"),
            CodeText = Color.Parse("#E8D5F5"),
            BlockquoteBorder = Color.Parse("#7B5EA7"),
            LinkColor = Color.Parse("#BB86FC"),
            TableBorder = Color.Parse("#5A3E7A"),
            MutedText = Color.Parse("#B39DDB"),
            RuleBrush = Color.Parse("#7B5EA7"),
            Nat1Brush = Color.Parse("#FF6B6B"),
            Nat20Brush = Color.Parse("#69F0AE"),
            Nat1Bg = Colors.Transparent,
            Nat20Bg = Colors.Transparent,
            OverlayBg = Color.Parse("#CC0D0A1A"),
            DialogBg = Color.Parse("#FF1D1433"),
            ActiveHighlight = Color.Parse("#409B59B6"),
            HoverHighlight = Color.Parse("#20E8D5F5"),
            Surface = Color.Parse("#241A3A"),
            Accent = Color.Parse("#9B59B6"),
            AccentForeground = Color.Parse("#FFFFFF")
        }
    };

    public AppTheme CurrentTheme { get; private set; }
    public double CurrentScale { get; private set; } = 1.0;

    public event Action? ThemeChanged;
    public event Action? ScaleChanged;

    public ThemeService() {
        CurrentTheme = AvailableThemes[0];
    }

    public void ApplyTheme(string themeId) {
        var theme = AvailableThemes.FirstOrDefault(t => t.Id == themeId);
        if (theme == null) return;

        CurrentTheme = theme;

        var app = Application.Current;
        if (app == null) return;

        if (themeId == SystemThemeId) {
            ApplySystemTheme(app);
        } else {
            UnsubscribeFromSystemTheme(app);
            ApplyCustomTheme(app, theme);
        }

        ThemeChanged?.Invoke();
    }

    private void ApplySystemTheme(Application app) {
        app.RequestedThemeVariant = ThemeVariant.Default;

        // Clear custom accent overrides so Fluent uses its OS/built-in defaults
        app.Resources.Remove("SystemAccentColor");
        app.Resources.Remove("SystemAccentColorDark1");
        app.Resources.Remove("SystemAccentColorDark2");
        app.Resources.Remove("SystemAccentColorDark3");
        app.Resources.Remove("SystemAccentColorLight1");
        app.Resources.Remove("SystemAccentColorLight2");
        app.Resources.Remove("SystemAccentColorLight3");

        var isDark = app.ActualThemeVariant == ThemeVariant.Dark;
        ApplySystemColors(app, isDark);

        if (!_subscribedToSystemTheme) {
            app.ActualThemeVariantChanged += OnSystemThemeChanged;
            _subscribedToSystemTheme = true;
        }
    }

    private void OnSystemThemeChanged(object? sender, EventArgs e) {
        var app = Application.Current;
        if (app == null || CurrentTheme.Id != SystemThemeId) return;

        var isDark = app.ActualThemeVariant == ThemeVariant.Dark;
        ApplySystemColors(app, isDark);
        ThemeChanged?.Invoke();
    }

    private void ApplySystemColors(Application app, bool isDark) {
        if (_themeResources != null) {
            app.Resources.MergedDictionaries.Remove(_themeResources);
        }

        _themeResources = new ResourceDictionary();

        if (isDark) {
            AddBrush(_themeResources, "DnDCodeBlockBg", Color.Parse("#1A1A1A"));
            AddBrush(_themeResources, "DnDInlineCodeBg", Color.Parse("#2A2A2A"));
            AddBrush(_themeResources, "DnDCodeText", Color.Parse("#E0E0E0"));
            AddBrush(_themeResources, "DnDBlockquoteBorder", Color.Parse("#555555"));
            AddBrush(_themeResources, "DnDLinkColor", Color.Parse("#60CDFF"));
            AddBrush(_themeResources, "DnDTableBorder", Color.Parse("#444444"));
            AddBrush(_themeResources, "DnDMutedText", Color.Parse("#999999"));
            AddBrush(_themeResources, "DnDRuleBrush", Color.Parse("#555555"));
            AddBrush(_themeResources, "DnDNat1Brush", Color.Parse("#FF4444"));
            AddBrush(_themeResources, "DnDNat20Brush", Color.Parse("#44DD44"));
            AddBrush(_themeResources, "DnDNat1Bg", Colors.Transparent);
            AddBrush(_themeResources, "DnDNat20Bg", Colors.Transparent);
            AddBrush(_themeResources, "DnDOverlayBg", Color.Parse("#CC000000"));
            AddBrush(_themeResources, "DnDDialogBg", Color.Parse("#FF202020"));
            AddBrush(_themeResources, "DnDActiveHighlight", Color.Parse("#4060CDFF"));
            AddBrush(_themeResources, "DnDHoverHighlight", Color.Parse("#30FFFFFF"));
            AddBrush(_themeResources, "DnDSurface", Color.Parse("#202020"));
            AddBrush(_themeResources, "DnDAccent", Color.Parse("#60CDFF"));
            AddBrush(_themeResources, "DnDAccentForeground", Color.Parse("#003E6E"));
            AddBrush(_themeResources, "DnDBorderBrush", Color.Parse("#999999"));
            AddBrush(_themeResources, "DnDLabelForeground", Color.Parse("#999999"));
            AddBrush(_themeResources, "DnDErrorForeground", Color.Parse("#FF4444"));
            AddBrush(_themeResources, "DnDWarningForeground", Color.Parse("#FCE100"));
        } else {
            AddBrush(_themeResources, "DnDCodeBlockBg", Color.Parse("#F0F0F0"));
            AddBrush(_themeResources, "DnDInlineCodeBg", Color.Parse("#E8E8E8"));
            AddBrush(_themeResources, "DnDCodeText", Color.Parse("#1E1E1E"));
            AddBrush(_themeResources, "DnDBlockquoteBorder", Color.Parse("#CCCCCC"));
            AddBrush(_themeResources, "DnDLinkColor", Color.Parse("#005FB8"));
            AddBrush(_themeResources, "DnDTableBorder", Color.Parse("#CCCCCC"));
            AddBrush(_themeResources, "DnDMutedText", Color.Parse("#666666"));
            AddBrush(_themeResources, "DnDRuleBrush", Color.Parse("#CCCCCC"));
            AddBrush(_themeResources, "DnDNat1Brush", Color.Parse("#FFFFFF"));
            AddBrush(_themeResources, "DnDNat20Brush", Color.Parse("#FFFFFF"));
            AddBrush(_themeResources, "DnDNat1Bg", Color.Parse("#C42B1C"));
            AddBrush(_themeResources, "DnDNat20Bg", Color.Parse("#0F7B0F"));
            AddBrush(_themeResources, "DnDOverlayBg", Color.Parse("#80000000"));
            AddBrush(_themeResources, "DnDDialogBg", Color.Parse("#FFF3F3F3"));
            AddBrush(_themeResources, "DnDActiveHighlight", Color.Parse("#40005FB8"));
            AddBrush(_themeResources, "DnDHoverHighlight", Color.Parse("#20000000"));
            AddBrush(_themeResources, "DnDSurface", Color.Parse("#F3F3F3"));
            AddBrush(_themeResources, "DnDAccent", Color.Parse("#005FB8"));
            AddBrush(_themeResources, "DnDAccentForeground", Color.Parse("#FFFFFF"));
            AddBrush(_themeResources, "DnDBorderBrush", Color.Parse("#666666"));
            AddBrush(_themeResources, "DnDLabelForeground", Color.Parse("#666666"));
            AddBrush(_themeResources, "DnDErrorForeground", Color.Parse("#C42B1C"));
            AddBrush(_themeResources, "DnDWarningForeground", Color.Parse("#9D5D00"));
        }

        app.Resources.MergedDictionaries.Add(_themeResources);
    }

    private void UnsubscribeFromSystemTheme(Application app) {
        if (_subscribedToSystemTheme) {
            app.ActualThemeVariantChanged -= OnSystemThemeChanged;
            _subscribedToSystemTheme = false;
        }
    }

    private void ApplyCustomTheme(Application app, AppTheme theme) {
        app.RequestedThemeVariant = theme.BaseVariant;

        // Override Fluent theme accent color ramp
        app.Resources["SystemAccentColor"] = theme.Accent;
        app.Resources["SystemAccentColorDark1"] = AdjustBrightness(theme.Accent, -0.15);
        app.Resources["SystemAccentColorDark2"] = AdjustBrightness(theme.Accent, -0.30);
        app.Resources["SystemAccentColorDark3"] = AdjustBrightness(theme.Accent, -0.45);
        app.Resources["SystemAccentColorLight1"] = AdjustBrightness(theme.Accent, 0.15);
        app.Resources["SystemAccentColorLight2"] = AdjustBrightness(theme.Accent, 0.30);
        app.Resources["SystemAccentColorLight3"] = AdjustBrightness(theme.Accent, 0.45);

        if (_themeResources != null) {
            app.Resources.MergedDictionaries.Remove(_themeResources);
        }

        _themeResources = new ResourceDictionary();
        AddBrush(_themeResources, "DnDCodeBlockBg", theme.CodeBlockBg);
        AddBrush(_themeResources, "DnDInlineCodeBg", theme.InlineCodeBg);
        AddBrush(_themeResources, "DnDCodeText", theme.CodeText);
        AddBrush(_themeResources, "DnDBlockquoteBorder", theme.BlockquoteBorder);
        AddBrush(_themeResources, "DnDLinkColor", theme.LinkColor);
        AddBrush(_themeResources, "DnDTableBorder", theme.TableBorder);
        AddBrush(_themeResources, "DnDMutedText", theme.MutedText);
        AddBrush(_themeResources, "DnDRuleBrush", theme.RuleBrush);
        AddBrush(_themeResources, "DnDNat1Brush", theme.Nat1Brush);
        AddBrush(_themeResources, "DnDNat20Brush", theme.Nat20Brush);
        AddBrush(_themeResources, "DnDNat1Bg", theme.Nat1Bg);
        AddBrush(_themeResources, "DnDNat20Bg", theme.Nat20Bg);
        AddBrush(_themeResources, "DnDOverlayBg", theme.OverlayBg);
        AddBrush(_themeResources, "DnDDialogBg", theme.DialogBg);
        AddBrush(_themeResources, "DnDActiveHighlight", theme.ActiveHighlight);
        AddBrush(_themeResources, "DnDHoverHighlight", theme.HoverHighlight);
        AddBrush(_themeResources, "DnDSurface", theme.Surface);
        AddBrush(_themeResources, "DnDAccent", theme.Accent);
        AddBrush(_themeResources, "DnDAccentForeground", theme.AccentForeground);

        // Semantic aliases (reuse existing theme colors)
        AddBrush(_themeResources, "DnDBorderBrush", theme.MutedText);
        AddBrush(_themeResources, "DnDLabelForeground", theme.MutedText);
        AddBrush(_themeResources, "DnDErrorForeground", theme.Nat1Brush);
        AddBrush(_themeResources, "DnDWarningForeground", theme.Accent);

        app.Resources.MergedDictionaries.Add(_themeResources);
    }

    public void SetScale(double scale) {
        CurrentScale = Math.Clamp(scale, 0.5, 2.0);
        ScaleChanged?.Invoke();
    }

    private static void AddBrush(ResourceDictionary dict, string key, Color color) {
        dict[key] = new SolidColorBrush(color);
    }

    private static Color AdjustBrightness(Color color, double amount) {
        int r = Math.Clamp((int)(color.R + 255 * amount), 0, 255);
        int g = Math.Clamp((int)(color.G + 255 * amount), 0, 255);
        int b = Math.Clamp((int)(color.B + 255 * amount), 0, 255);
        return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
    }
}
