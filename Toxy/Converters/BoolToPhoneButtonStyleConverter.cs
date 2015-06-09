using System;
using System.Windows;
using System.Windows.Data;

namespace Toxy.Converters
{
    public class BoolToPhoneButtonStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Styles.xaml");

            if (values.Length != 3)
                return dic["ToxGreenButtonStyle"];

            bool? isCalling = values[0] as bool?;
            bool? isRinging = values[1] as bool?;
            bool? isCallInProgress = values[2] as bool?;

            if (isCalling == null || isCallInProgress == null)
                return dic["ToxGreenButtonStyle"];

            if (isCalling == true || isRinging == true)
                return dic["ToxYellowButtonStyle"];

            if (isCallInProgress == true)
                return dic["ToxRedButtonStyle"];

            return dic["ToxGreenButtonStyle"];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
