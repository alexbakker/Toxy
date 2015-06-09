using System;
using System.Windows;
using System.Windows.Data;

namespace Toxy.Converters
{
    public class BoolToMessageForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Colors.xaml");

            bool? received = value as bool?;
            if (received == null || received == true)
                return dic["ToxDarkGreyBrush"];

            return dic["ToxLightGreyBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
