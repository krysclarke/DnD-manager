using Avalonia.Media;
using Avalonia.Styling;

namespace DnDManager.Models;

public class AppTheme {
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required ThemeVariant BaseVariant { get; init; }

    // Markdown / code
    public required Color CodeBlockBg { get; init; }
    public required Color InlineCodeBg { get; init; }
    public required Color CodeText { get; init; }
    public required Color BlockquoteBorder { get; init; }
    public required Color LinkColor { get; init; }
    public required Color TableBorder { get; init; }
    public required Color MutedText { get; init; }
    public required Color RuleBrush { get; init; }

    // Dice
    public required Color Nat1Brush { get; init; }
    public required Color Nat20Brush { get; init; }
    public required Color Nat1Bg { get; init; }
    public required Color Nat20Bg { get; init; }

    // UI chrome
    public required Color OverlayBg { get; init; }
    public required Color DialogBg { get; init; }
    public required Color ActiveHighlight { get; init; }
    public required Color HoverHighlight { get; init; }
    public required Color Surface { get; init; }
    public required Color Accent { get; init; }
    public required Color AccentForeground { get; init; }

    // Fonts
    public string UiFont { get; init; } = "Default";
    public double UiFontSize { get; init; } = 14;
    public string HeadingFont { get; init; } = "Default";
    public double HeadingFontSize { get; init; } = 18;
    public string MonospaceFont { get; init; } = "Cascadia Mono, Consolas, monospace";
    public double MonospaceFontSize { get; init; } = 14;
    public string DiceFont { get; init; } = "Default";
    public double DiceFontSize { get; init; } = 14;

    // Custom theme flag
    public bool IsBuiltIn { get; init; } = true;
}
