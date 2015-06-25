using System;
using System.Windows;
using System.Windows.Data;
using Toxy.Managers;

namespace Toxy.Converters
{
    public class BoolToPhoneButtonStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Styles.xaml");

            var state = (CallState)value;

            if (state.HasFlag(CallState.Ringing) || state.HasFlag(CallState.Calling))
                return dic["ToxYellowButtonStyle"];

            if (state.HasFlag(CallState.None))
                return dic["ToxGreenButtonStyle"];

            if (state.HasFlag(CallState.InProgress))
                return dic["ToxRedButtonStyle"];

            return dic["ToxGreenButtonStyle"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
