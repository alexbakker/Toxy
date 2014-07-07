using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Toxy.Converter
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public BoolToVisibilityConverter()
        {
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
        }

        public Visibility TrueValue { get; set; }
        public Visibility FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bValue = false;
            var tmp = value as bool?;
            if (tmp.HasValue)
            {
                bValue = tmp.Value;
            }
            return (bValue) ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility && (Visibility)value == TrueValue;
        }
    }
}