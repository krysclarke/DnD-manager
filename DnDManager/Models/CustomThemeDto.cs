using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using Avalonia.Styling;

namespace DnDManager.Models;

public class CustomThemeDto {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("baseVariant")]
    public string BaseVariant { get; set; } = "Dark";

    // Colors (stored as hex strings)
    [JsonPropertyName("codeBlockBg")]
    public string CodeBlockBg { get; set; } = string.Empty;

    [JsonPropertyName("inlineCodeBg")]
    public string InlineCodeBg { get; set; } = string.Empty;

    [JsonPropertyName("codeText")]
    public string CodeText { get; set; } = string.Empty;

    [JsonPropertyName("blockquoteBorder")]
    public string BlockquoteBorder { get; set; } = string.Empty;

    [JsonPropertyName("linkColor")]
    public string LinkColor { get; set; } = string.Empty;

    [JsonPropertyName("tableBorder")]
    public string TableBorder { get; set; } = string.Empty;

    [JsonPropertyName("mutedText")]
    public string MutedText { get; set; } = string.Empty;

    [JsonPropertyName("ruleBrush")]
    public string RuleBrush { get; set; } = string.Empty;

    [JsonPropertyName("nat1Brush")]
    public string Nat1Brush { get; set; } = string.Empty;

    [JsonPropertyName("nat20Brush")]
    public string Nat20Brush { get; set; } = string.Empty;

    [JsonPropertyName("nat1Bg")]
    public string Nat1Bg { get; set; } = string.Empty;

    [JsonPropertyName("nat20Bg")]
    public string Nat20Bg { get; set; } = string.Empty;

    [JsonPropertyName("overlayBg")]
    public string OverlayBg { get; set; } = string.Empty;

    [JsonPropertyName("dialogBg")]
    public string DialogBg { get; set; } = string.Empty;

    [JsonPropertyName("activeHighlight")]
    public string ActiveHighlight { get; set; } = string.Empty;

    [JsonPropertyName("hoverHighlight")]
    public string HoverHighlight { get; set; } = string.Empty;

    [JsonPropertyName("surface")]
    public string Surface { get; set; } = string.Empty;

    [JsonPropertyName("accent")]
    public string Accent { get; set; } = string.Empty;

    [JsonPropertyName("accentForeground")]
    public string AccentForeground { get; set; } = string.Empty;

    // Fonts
    [JsonPropertyName("uiFont")]
    public string UiFont { get; set; } = "Default";

    [JsonPropertyName("uiFontSize")]
    public double UiFontSize { get; set; } = 14;

    [JsonPropertyName("headingFont")]
    public string HeadingFont { get; set; } = "Default";

    [JsonPropertyName("headingFontSize")]
    public double HeadingFontSize { get; set; } = 18;

    [JsonPropertyName("monospaceFont")]
    public string MonospaceFont { get; set; } = "Cascadia Mono, Consolas, monospace";

    [JsonPropertyName("monospaceFontSize")]
    public double MonospaceFontSize { get; set; } = 14;

    [JsonPropertyName("diceFont")]
    public string DiceFont { get; set; } = "Default";

    [JsonPropertyName("diceFontSize")]
    public double DiceFontSize { get; set; } = 14;

    private static string ColorToHex(Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    public static CustomThemeDto FromAppTheme(AppTheme theme) => new() {
        Id = theme.Id,
        DisplayName = theme.DisplayName,
        BaseVariant = theme.BaseVariant == ThemeVariant.Light ? "Light" : "Dark",
        CodeBlockBg = ColorToHex(theme.CodeBlockBg),
        InlineCodeBg = ColorToHex(theme.InlineCodeBg),
        CodeText = ColorToHex(theme.CodeText),
        BlockquoteBorder = ColorToHex(theme.BlockquoteBorder),
        LinkColor = ColorToHex(theme.LinkColor),
        TableBorder = ColorToHex(theme.TableBorder),
        MutedText = ColorToHex(theme.MutedText),
        RuleBrush = ColorToHex(theme.RuleBrush),
        Nat1Brush = ColorToHex(theme.Nat1Brush),
        Nat20Brush = ColorToHex(theme.Nat20Brush),
        Nat1Bg = ColorToHex(theme.Nat1Bg),
        Nat20Bg = ColorToHex(theme.Nat20Bg),
        OverlayBg = ColorToHex(theme.OverlayBg),
        DialogBg = ColorToHex(theme.DialogBg),
        ActiveHighlight = ColorToHex(theme.ActiveHighlight),
        HoverHighlight = ColorToHex(theme.HoverHighlight),
        Surface = ColorToHex(theme.Surface),
        Accent = ColorToHex(theme.Accent),
        AccentForeground = ColorToHex(theme.AccentForeground),
        UiFont = theme.UiFont,
        UiFontSize = theme.UiFontSize,
        HeadingFont = theme.HeadingFont,
        HeadingFontSize = theme.HeadingFontSize,
        MonospaceFont = theme.MonospaceFont,
        MonospaceFontSize = theme.MonospaceFontSize,
        DiceFont = theme.DiceFont,
        DiceFontSize = theme.DiceFontSize
    };

    public AppTheme ToAppTheme() => new() {
        Id = Id,
        DisplayName = DisplayName,
        BaseVariant = BaseVariant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
        CodeBlockBg = Color.Parse(CodeBlockBg),
        InlineCodeBg = Color.Parse(InlineCodeBg),
        CodeText = Color.Parse(CodeText),
        BlockquoteBorder = Color.Parse(BlockquoteBorder),
        LinkColor = Color.Parse(LinkColor),
        TableBorder = Color.Parse(TableBorder),
        MutedText = Color.Parse(MutedText),
        RuleBrush = Color.Parse(RuleBrush),
        Nat1Brush = Color.Parse(Nat1Brush),
        Nat20Brush = Color.Parse(Nat20Brush),
        Nat1Bg = Color.Parse(Nat1Bg),
        Nat20Bg = Color.Parse(Nat20Bg),
        OverlayBg = Color.Parse(OverlayBg),
        DialogBg = Color.Parse(DialogBg),
        ActiveHighlight = Color.Parse(ActiveHighlight),
        HoverHighlight = Color.Parse(HoverHighlight),
        Surface = Color.Parse(Surface),
        Accent = Color.Parse(Accent),
        AccentForeground = Color.Parse(AccentForeground),
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

    public static string SerializeList(List<CustomThemeDto> themes) =>
        JsonSerializer.Serialize(themes);

    public static List<CustomThemeDto> DeserializeList(string json) =>
        JsonSerializer.Deserialize<List<CustomThemeDto>>(json) ?? [];
}
