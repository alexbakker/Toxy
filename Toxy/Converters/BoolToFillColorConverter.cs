using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Toxy.Converters
{
    public class BoolToFillColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Colors.xaml");

            if (values[0] != null)
                return Brushes.Transparent;

            if (values.Length != 2)
                return dic["ToxMediumGreyBrush"];

            bool? selected = values[1] as bool?;
            if (selected == null)
                return dic["ToxWhiteBrush"];

            if (selected == true)
                return dic["ToxMediumGreyBrush"];

            return dic["ToxWhiteBrush"];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
