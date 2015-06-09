using SharpTox.Core;
using System;
using System.Windows;
using System.Windows.Data;

namespace Toxy.Converters
{
    public class ToxUserStatusToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Colors.xaml");

            if (values.Length != 2)
                return dic["ToxDotOfflineBrush"];

            var userStatus = (ToxUserStatus)values[0];
            var connStatus = (ToxConnectionStatus)values[1];

            var icon = dic[GetColor(userStatus, connStatus)];
            if (icon == null)
                return dic["ToxDotOfflineBrush"];

            return icon;
        }

        private static string GetColor(ToxUserStatus status, ToxConnectionStatus connStatus)
        {
            if (connStatus == ToxConnectionStatus.None)
                return "ToxDotOfflineBrush";

            switch (status)
            {
                case ToxUserStatus.None:
                    return "ToxDotOnlineBrush";
                case ToxUserStatus.Away:
                    return "ToxDotIdleBrush";
                case ToxUserStatus.Busy:
                    return "ToxDotBusyBrush";
                default:
                    return "ToxDotOnlineBrush";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
