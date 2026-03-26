using Avalonia.Media;

namespace DnDManager.Services;

public static class WcagContrastChecker {
    public static double GetRelativeLuminance(Color c) {
        var r = Linearize(c.R / 255.0);
        var g = Linearize(c.G / 255.0);
        var b = Linearize(c.B / 255.0);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    public static double GetContrastRatio(Color fg, Color bg) {
        var l1 = GetRelativeLuminance(fg);
        var l2 = GetRelativeLuminance(bg);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    public static WcagResult Check(Color fg, Color bg) {
        var ratio = GetContrastRatio(fg, bg);
        return new WcagResult {
            Ratio = ratio,
            MeetsAA = ratio >= 4.5,
            MeetsAAA = ratio >= 7.0
        };
    }

    private static double Linearize(double channel) {
        return channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}

public record WcagResult {
    public required double Ratio { get; init; }
    public required bool MeetsAA { get; init; }
    public required bool MeetsAAA { get; init; }
}
