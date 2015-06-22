using System;
using System.Windows;
using System.Windows.Data;

namespace Toxy.Converters
{
    public class BoolToVideoButtonStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Styles.xaml");

            if (values.Length != 4)
                return dic["ToxGreenButtonStyle"];

            bool? isCalling = values[0] as bool?;
            bool? isRinging = values[1] as bool?;
            bool? isCallInProgress = values[2] as bool?;
            bool? isVideoEnabled = values[3] as bool?;

            if (isCalling == true || isRinging == true)
                return dic["ToxYellowButtonStyle"];

            if (isCallInProgress == true && isVideoEnabled == true)
                return dic["ToxRedButtonStyle"];
            else if (isCallInProgress == true)
                return dic["ToxGreenButtonStyle"];

            return dic["ToxGreenButtonStyle"];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
