using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DnDManager.Converters;

public class BoolToBrushConverter : IValueConverter {
    public string? TrueResourceKey { get; set; }
    public IBrush? FallbackTrueBrush { get; set; }
    public IBrush? TrueBrush { get; set; }
    public IBrush? FalseBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not true) return FalseBrush;

        if (TrueResourceKey != null
            && Application.Current?.Resources.TryGetResource(TrueResourceKey, null, out var resource) == true
            && resource is IBrush brush) {
            return brush;
        }

        return TrueBrush ?? FallbackTrueBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}
