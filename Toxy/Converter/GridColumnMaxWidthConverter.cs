using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Toxy.Converter
{
    class GridColumnMaxWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double minWidth;
            if (value is double && parameter is string && double.TryParse((string)parameter, NumberStyles.Any, CultureInfo.InvariantCulture, out minWidth))
            {
                var maxWidth = (double)value - minWidth;
                return maxWidth < 0 ? 0 : maxWidth;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
