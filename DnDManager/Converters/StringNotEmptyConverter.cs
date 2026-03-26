using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DnDManager.Converters;

public class StringNotEmptyConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is string s && s.Length > 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}
