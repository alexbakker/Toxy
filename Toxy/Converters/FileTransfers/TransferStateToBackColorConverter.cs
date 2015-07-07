using System;
using System.Windows;
using System.Windows.Data;
using Toxy.Managers;

namespace Toxy.Converters.FileTransfers
{
    public class TransferStateToBackColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Colors.xaml");

            var inProgress = values[0] as bool?;
            var isPaused = values[1] as bool?;
            var isCancelled = values[2] as bool?;

            if (isPaused == true)
                return dic["ToxYellowBrush"];
            else if (isCancelled == true)
                return dic["ToxRedBrush"];
            else
                return dic["ToxLightGreyBrush"];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
