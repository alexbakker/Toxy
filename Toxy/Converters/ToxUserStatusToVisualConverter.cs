using SharpTox.Core;
using System;
using System.Windows;
using System.Windows.Data;
using Toxy.ViewModels;

namespace Toxy.Converters
{
    public class ToxUserStatusToVisualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Icons.xaml");

            if (values.Length < 2)
                return dic["tox_dot_offline"];

            var userStatus = (ToxUserStatus)values[0];
            var connStatus = (ToxConnectionStatus)values[1];
            bool hasUnreadMessages = values.Length != 3 ? false : (bool)values[2];

            var icon = dic[GetIconName(userStatus, connStatus, hasUnreadMessages)];
            if (icon == null)
                return dic["tox_dot_offline"];

            return icon;
        }

        private static string GetIconName(ToxUserStatus status, ToxConnectionStatus connStatus, bool hasUnreadMessages)
        {
            string result;

            if (connStatus == ToxConnectionStatus.None)
            {
                result = "tox_dot_offline";
            }
            else
            {
                switch (status)
                {
                    case ToxUserStatus.None:
                        result = "tox_dot_online";
                        break;
                    case ToxUserStatus.Away:
                        result = "tox_dot_idle";
                        break;
                    case ToxUserStatus.Busy:
                        result = "tox_dot_busy";
                        break;
                    default:
                        result = "tox_dot_online"; //we don't know about this status, just show 'online'
                        break;
                }
            }

            return result + (hasUnreadMessages ? "_notification" : string.Empty);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
