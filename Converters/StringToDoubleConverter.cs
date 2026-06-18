using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WeightBalance.Converters;

public class StringToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double d ? d.ToString("F1", CultureInfo.InvariantCulture) : "0.0";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        return 0.0;
    }
}
