using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DnDManager.Converters;

public class NatRollFontWeightConverter : IMultiValueConverter {
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
        if (values.Count < 2) return FontWeight.Normal;

        var isNat1 = values[0] is true;
        var isNat20 = values[1] is true;

        return isNat1 || isNat20 ? FontWeight.Bold : FontWeight.Normal;
    }
}
