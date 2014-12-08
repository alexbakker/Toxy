using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Toxy.Converter
{
    public class IgnoreStatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return (bool)value ? "Unignore" : "Ignore";
            else
                return "Ignore";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
