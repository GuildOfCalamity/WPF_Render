using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFRender;

public class BoolToReverseConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var val = (bool)value;
        return !val;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var val = (bool)value;
        if (parameter is string param && (param.ToString().Equals("inverse", StringComparison.OrdinalIgnoreCase) || param.ToString().Equals("reverse", StringComparison.OrdinalIgnoreCase) || param.ToString().Equals("opposite", StringComparison.OrdinalIgnoreCase)))
            val = !val;
        return val ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}