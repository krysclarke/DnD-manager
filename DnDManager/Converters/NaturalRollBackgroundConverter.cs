using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DnDManager.Converters;

public class NaturalRollBackgroundConverter : IMultiValueConverter {
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
        if (values.Count < 2) return Brushes.Transparent;

        var isNat1 = values[0] is true;
        var isNat20 = values[1] is true;

        if (isNat1) return GetBrush("DnDNat1Bg", Brushes.Transparent);
        if (isNat20) return GetBrush("DnDNat20Bg", Brushes.Transparent);

        return Brushes.Transparent;
    }

    private static IBrush GetBrush(string key, IBrush fallback) {
        if (Application.Current?.Resources.TryGetResource(key, null, out var resource) == true
            && resource is IBrush brush) {
            return brush;
        }
        return fallback;
    }
}
