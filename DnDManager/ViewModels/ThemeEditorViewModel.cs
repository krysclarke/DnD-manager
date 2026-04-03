using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class ThemeEditorViewModel : ObservableObject {
    private readonly IThemeService _themeService;
    private readonly ICampaignRepository _campaignRepository;
    private bool _isSuppressingUpdates;

    [ObservableProperty]
    private bool _isEditorVisible;

    [ObservableProperty]
    private bool _isAdvancedColorMode;

    [ObservableProperty]
    private bool _isAdvancedFontMode;

    [ObservableProperty]
    private string _themeName = string.Empty;

    [ObservableProperty]
    private bool _isDarkBase = true;

    // --- Colors ---
    [ObservableProperty] private Color _surface;
    [ObservableProperty] private Color _dialogBg;
    [ObservableProperty] private Color _overlayBg;
    [ObservableProperty] private Color _codeBlockBg;
    [ObservableProperty] private Color _inlineCodeBg;
    [ObservableProperty] private Color _accent;
    [ObservableProperty] private Color _accentForeground;
    [ObservableProperty] private Color _activeHighlight;
    [ObservableProperty] private Color _hoverHighlight;
    [ObservableProperty] private Color _mutedText;
    [ObservableProperty] private Color _codeText;
    [ObservableProperty] private Color _linkColor;
    [ObservableProperty] private Color _blockquoteBorder;
    [ObservableProperty] private Color _tableBorder;
    [ObservableProperty] private Color _ruleBrush;
    [ObservableProperty] private Color _nat1Brush;
    [ObservableProperty] private Color _nat1Bg;
    [ObservableProperty] private Color _nat20Brush;
    [ObservableProperty] private Color _nat20Bg;

    // --- Fonts ---
    [ObservableProperty] private string _uiFont = "Default";
    [ObservableProperty] private double _uiFontSize = 14;
    [ObservableProperty] private string _headingFont = "Default";
    [ObservableProperty] private double _headingFontSize = 18;
    [ObservableProperty] private string _monospaceFont = "Cascadia Mono, Consolas, monospace";
    [ObservableProperty] private double _monospaceFontSize = 14;
    [ObservableProperty] private string _diceFont = "Default";
    [ObservableProperty] private double _diceFontSize = 14;

    // Basic font mode properties (set all categories at once)
    [ObservableProperty] private string _globalFont = "Default";
    [ObservableProperty] private double _globalFontSize = 14;

    public ObservableCollection<string> AvailableFonts { get; } = new();

    public string? EditingThemeId { get; private set; }

    public ObservableCollection<ColorGroupViewModel> ColorGroups { get; } = new();

    public ThemeEditorViewModel(IThemeService themeService, ICampaignRepository campaignRepository) {
        _themeService = themeService;
        _campaignRepository = campaignRepository;
        InitializeColorGroups();
        LoadSystemFonts();
    }

    private void InitializeColorGroups() {
        ColorGroups.Add(new ColorGroupViewModel("Backgrounds", "Base background colors for different surface layers", new[] {
            new ColorEntryViewModel("Surface", () => Surface, v => Surface = v),
            new ColorEntryViewModel("Dialog Background", () => DialogBg, v => DialogBg = v),
            new ColorEntryViewModel("Overlay Background", () => OverlayBg, v => OverlayBg = v),
            new ColorEntryViewModel("Code Block Background", () => CodeBlockBg, v => CodeBlockBg = v),
            new ColorEntryViewModel("Inline Code Background", () => InlineCodeBg, v => InlineCodeBg = v),
        }));
        ColorGroups.Add(new ColorGroupViewModel("Accent & Highlights", "Primary accent color and interactive states", new[] {
            new ColorEntryViewModel("Accent", () => Accent, v => Accent = v),
            new ColorEntryViewModel("Accent Foreground", () => AccentForeground, v => AccentForeground = v),
            new ColorEntryViewModel("Active Highlight", () => ActiveHighlight, v => ActiveHighlight = v),
            new ColorEntryViewModel("Hover Highlight", () => HoverHighlight, v => HoverHighlight = v),
        }));
        ColorGroups.Add(new ColorGroupViewModel("Text", "Text colors for different contexts", new[] {
            new ColorEntryViewModel("Muted Text", () => MutedText, v => MutedText = v),
            new ColorEntryViewModel("Code Text", () => CodeText, v => CodeText = v),
            new ColorEntryViewModel("Link Color", () => LinkColor, v => LinkColor = v),
        }));
        ColorGroups.Add(new ColorGroupViewModel("Borders & Dividers", "Lines, borders, and separators", new[] {
            new ColorEntryViewModel("Blockquote Border", () => BlockquoteBorder, v => BlockquoteBorder = v),
            new ColorEntryViewModel("Table Border", () => TableBorder, v => TableBorder = v),
            new ColorEntryViewModel("Rule/Divider", () => RuleBrush, v => RuleBrush = v),
        }));
        ColorGroups.Add(new ColorGroupViewModel("Dice Rolls", "Natural 1 and natural 20 styling", new[] {
            new ColorEntryViewModel("Nat 1 Text", () => Nat1Brush, v => Nat1Brush = v),
            new ColorEntryViewModel("Nat 1 Background", () => Nat1Bg, v => Nat1Bg = v),
            new ColorEntryViewModel("Nat 20 Text", () => Nat20Brush, v => Nat20Brush = v),
            new ColorEntryViewModel("Nat 20 Background", () => Nat20Bg, v => Nat20Bg = v),
        }));
    }

    private void LoadSystemFonts() {
        AvailableFonts.Add("Default");
        try {
            var fontManager = Avalonia.Media.FontManager.Current;
            foreach (var font in fontManager.SystemFonts.OrderBy(f => f.Name)) {
                AvailableFonts.Add(font.Name);
            }
        } catch {
            // Fallback fonts if system font enumeration fails
            AvailableFonts.Add("Arial");
            AvailableFonts.Add("Times New Roman");
            AvailableFonts.Add("Cascadia Mono");
            AvailableFonts.Add("Consolas");
        }
    }

    public void LoadFromTheme(AppTheme theme) {
        _isSuppressingUpdates = true;
        try {
            EditingThemeId = theme.Id;
            ThemeName = theme.DisplayName;
            IsDarkBase = theme.BaseVariant == ThemeVariant.Dark;

            Surface = theme.Surface;
            DialogBg = theme.DialogBg;
            OverlayBg = theme.OverlayBg;
            CodeBlockBg = theme.CodeBlockBg;
            InlineCodeBg = theme.InlineCodeBg;
            Accent = theme.Accent;
            AccentForeground = theme.AccentForeground;
            ActiveHighlight = theme.ActiveHighlight;
            HoverHighlight = theme.HoverHighlight;
            MutedText = theme.MutedText;
            CodeText = theme.CodeText;
            LinkColor = theme.LinkColor;
            BlockquoteBorder = theme.BlockquoteBorder;
            TableBorder = theme.TableBorder;
            RuleBrush = theme.RuleBrush;
            Nat1Brush = theme.Nat1Brush;
            Nat1Bg = theme.Nat1Bg;
            Nat20Brush = theme.Nat20Brush;
            Nat20Bg = theme.Nat20Bg;

            UiFont = theme.UiFont;
            UiFontSize = theme.UiFontSize;
            HeadingFont = theme.HeadingFont;
            HeadingFontSize = theme.HeadingFontSize;
            MonospaceFont = theme.MonospaceFont;
            MonospaceFontSize = theme.MonospaceFontSize;
            DiceFont = theme.DiceFont;
            DiceFontSize = theme.DiceFontSize;

            GlobalFont = theme.UiFont;
            GlobalFontSize = theme.UiFontSize;

            // Refresh color group swatches
            foreach (var group in ColorGroups)
                group.Refresh();

            IsEditorVisible = true;
        } finally {
            _isSuppressingUpdates = false;
        }
    }

    public AppTheme BuildTheme() => new() {
        Id = EditingThemeId ?? Guid.NewGuid().ToString(),
        DisplayName = ThemeName,
        BaseVariant = IsDarkBase ? ThemeVariant.Dark : ThemeVariant.Light,
        Surface = Surface,
        DialogBg = DialogBg,
        OverlayBg = OverlayBg,
        CodeBlockBg = CodeBlockBg,
        InlineCodeBg = InlineCodeBg,
        Accent = Accent,
        AccentForeground = AccentForeground,
        ActiveHighlight = ActiveHighlight,
        HoverHighlight = HoverHighlight,
        MutedText = MutedText,
        CodeText = CodeText,
        LinkColor = LinkColor,
        BlockquoteBorder = BlockquoteBorder,
        TableBorder = TableBorder,
        RuleBrush = RuleBrush,
        Nat1Brush = Nat1Brush,
        Nat1Bg = Nat1Bg,
        Nat20Brush = Nat20Brush,
        Nat20Bg = Nat20Bg,
        UiFont = UiFont,
        UiFontSize = UiFontSize,
        HeadingFont = HeadingFont,
        HeadingFontSize = HeadingFontSize,
        MonospaceFont = MonospaceFont,
        MonospaceFontSize = MonospaceFontSize,
        DiceFont = DiceFont,
        DiceFontSize = DiceFontSize,
        IsBuiltIn = false
    };

    private void ApplyLivePreview() {
        if (_isSuppressingUpdates) return;

        var theme = BuildTheme();
        _themeService.ApplyThemeLive(theme);
        _themeService.UpdateCustomTheme(theme);
        _ = _themeService.SaveCustomThemesAsync(_campaignRepository);

        foreach (var group in ColorGroups)
            group.Refresh();
    }

    // Color property changes trigger live preview
    partial void OnSurfaceChanged(Color value) => ApplyLivePreview();
    partial void OnDialogBgChanged(Color value) => ApplyLivePreview();
    partial void OnOverlayBgChanged(Color value) => ApplyLivePreview();
    partial void OnCodeBlockBgChanged(Color value) => ApplyLivePreview();
    partial void OnInlineCodeBgChanged(Color value) => ApplyLivePreview();
    partial void OnAccentChanged(Color value) => ApplyLivePreview();
    partial void OnAccentForegroundChanged(Color value) => ApplyLivePreview();
    partial void OnActiveHighlightChanged(Color value) => ApplyLivePreview();
    partial void OnHoverHighlightChanged(Color value) => ApplyLivePreview();
    partial void OnMutedTextChanged(Color value) => ApplyLivePreview();
    partial void OnCodeTextChanged(Color value) => ApplyLivePreview();
    partial void OnLinkColorChanged(Color value) => ApplyLivePreview();
    partial void OnBlockquoteBorderChanged(Color value) => ApplyLivePreview();
    partial void OnTableBorderChanged(Color value) => ApplyLivePreview();
    partial void OnRuleBrushChanged(Color value) => ApplyLivePreview();
    partial void OnNat1BrushChanged(Color value) => ApplyLivePreview();
    partial void OnNat1BgChanged(Color value) => ApplyLivePreview();
    partial void OnNat20BrushChanged(Color value) => ApplyLivePreview();
    partial void OnNat20BgChanged(Color value) => ApplyLivePreview();

    // Font property changes trigger live preview
    partial void OnUiFontChanged(string value) => ApplyLivePreview();
    partial void OnUiFontSizeChanged(double value) => ApplyLivePreview();
    partial void OnHeadingFontChanged(string value) => ApplyLivePreview();
    partial void OnHeadingFontSizeChanged(double value) => ApplyLivePreview();
    partial void OnMonospaceFontChanged(string value) => ApplyLivePreview();
    partial void OnMonospaceFontSizeChanged(double value) => ApplyLivePreview();
    partial void OnDiceFontChanged(string value) => ApplyLivePreview();
    partial void OnDiceFontSizeChanged(double value) => ApplyLivePreview();

    partial void OnIsDarkBaseChanged(bool value) => ApplyLivePreview();

    partial void OnThemeNameChanged(string value) {
        if (_isSuppressingUpdates || EditingThemeId == null) return;
        var theme = BuildTheme();
        _themeService.UpdateCustomTheme(theme);
        _ = _themeService.SaveCustomThemesAsync(_campaignRepository);
    }

    // Basic font mode: setting global font/size updates all categories
    partial void OnGlobalFontChanged(string value) {
        if (_isSuppressingUpdates || IsAdvancedFontMode) return;
        _isSuppressingUpdates = true;
        UiFont = value;
        HeadingFont = value;
        DiceFont = value;
        _isSuppressingUpdates = false;
        ApplyLivePreview();
    }

    partial void OnGlobalFontSizeChanged(double value) {
        if (_isSuppressingUpdates || IsAdvancedFontMode) return;
        _isSuppressingUpdates = true;
        UiFontSize = value;
        HeadingFontSize = Math.Round(value * 1.286); // Maintain ~18/14 ratio
        MonospaceFontSize = value;
        DiceFontSize = value;
        _isSuppressingUpdates = false;
        ApplyLivePreview();
    }

    [RelayCommand]
    private void CreateNewTheme() {
        var baseTheme = _themeService.CurrentTheme;
        var id = Guid.NewGuid().ToString();
        var newTheme = new AppTheme {
            Id = id,
            DisplayName = "Custom Theme",
            BaseVariant = baseTheme.BaseVariant,
            Surface = baseTheme.Surface,
            DialogBg = baseTheme.DialogBg,
            OverlayBg = baseTheme.OverlayBg,
            CodeBlockBg = baseTheme.CodeBlockBg,
            InlineCodeBg = baseTheme.InlineCodeBg,
            Accent = baseTheme.Accent,
            AccentForeground = baseTheme.AccentForeground,
            ActiveHighlight = baseTheme.ActiveHighlight,
            HoverHighlight = baseTheme.HoverHighlight,
            MutedText = baseTheme.MutedText,
            CodeText = baseTheme.CodeText,
            LinkColor = baseTheme.LinkColor,
            BlockquoteBorder = baseTheme.BlockquoteBorder,
            TableBorder = baseTheme.TableBorder,
            RuleBrush = baseTheme.RuleBrush,
            Nat1Brush = baseTheme.Nat1Brush,
            Nat1Bg = baseTheme.Nat1Bg,
            Nat20Brush = baseTheme.Nat20Brush,
            Nat20Bg = baseTheme.Nat20Bg,
            UiFont = baseTheme.UiFont,
            UiFontSize = baseTheme.UiFontSize,
            HeadingFont = baseTheme.HeadingFont,
            HeadingFontSize = baseTheme.HeadingFontSize,
            MonospaceFont = baseTheme.MonospaceFont,
            MonospaceFontSize = baseTheme.MonospaceFontSize,
            DiceFont = baseTheme.DiceFont,
            DiceFontSize = baseTheme.DiceFontSize,
            IsBuiltIn = false
        };

        _themeService.AddCustomTheme(newTheme);
        _ = _themeService.SaveCustomThemesAsync(_campaignRepository);
        LoadFromTheme(newTheme);
        _themeService.ApplyThemeLive(newTheme);
        _ = _campaignRepository.SaveSettingAsync("theme", id);
        ThemeCreated?.Invoke(newTheme);
    }

    [RelayCommand]
    private void DeleteTheme() {
        if (EditingThemeId == null) return;
        var idToDelete = EditingThemeId;
        IsEditorVisible = false;
        EditingThemeId = null;
        _themeService.DeleteCustomTheme(idToDelete);
        _ = _themeService.SaveCustomThemesAsync(_campaignRepository);

        // Fall back to first built-in theme
        var fallback = _themeService.AvailableThemes[0];
        _themeService.ApplyTheme(fallback.Id);
        _ = _campaignRepository.SaveSettingAsync("theme", fallback.Id);
        ThemeDeleted?.Invoke(fallback);
    }

    public event Action<AppTheme>? ThemeCreated;
    public event Action<AppTheme>? ThemeDeleted;
}

public partial class ColorGroupViewModel : ObservableObject {
    public string Name { get; }
    public string Description { get; }
    public ObservableCollection<ColorEntryViewModel> Colors { get; }

    public ColorGroupViewModel(string name, string description, IEnumerable<ColorEntryViewModel> colors) {
        Name = name;
        Description = description;
        Colors = new ObservableCollection<ColorEntryViewModel>(colors);
    }

    public void Refresh() {
        foreach (var color in Colors)
            color.Refresh();
    }
}

public partial class ColorEntryViewModel : ObservableObject {
    private readonly Func<Color> _getter;
    private readonly Action<Color> _setter;
    private bool _isUpdating;

    public string Label { get; }

    [ObservableProperty]
    private Color _colorValue;

    [ObservableProperty]
    private string _hexValue = string.Empty;

    public ColorEntryViewModel(string label, Func<Color> getter, Action<Color> setter) {
        Label = label;
        _getter = getter;
        _setter = setter;
        _colorValue = getter();
        _hexValue = ColorToHex(_colorValue);
    }

    public void Refresh() {
        _isUpdating = true;
        ColorValue = _getter();
        HexValue = ColorToHex(ColorValue);
        _isUpdating = false;
    }

    partial void OnColorValueChanged(Color value) {
        if (_isUpdating) return;
        _isUpdating = true;
        HexValue = ColorToHex(value);
        _setter(value);
        _isUpdating = false;
    }

    partial void OnHexValueChanged(string value) {
        if (_isUpdating) return;
        try {
            var color = Color.Parse(value);
            _isUpdating = true;
            ColorValue = color;
            _setter(color);
            _isUpdating = false;
        } catch {
            // Invalid hex — ignore
        }
    }

    private static string ColorToHex(Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
}
