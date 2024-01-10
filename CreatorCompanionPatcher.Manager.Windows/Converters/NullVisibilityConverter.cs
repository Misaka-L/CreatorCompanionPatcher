using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CreatorCompanionPatcher.Manager.Windows.Converters;

public class NullVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!(targetType != typeof(Visibility) || targetType != typeof(object)))
            return DependencyProperty.UnsetValue;

        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}