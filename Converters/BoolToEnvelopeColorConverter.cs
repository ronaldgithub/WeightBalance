using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WeightBalance.Converters;

public class BoolToEnvelopeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromArgb(80, 0, 200, 0))
            : new SolidColorBrush(Color.FromArgb(80, 220, 50, 0));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
