using System;
using System.Windows;
using System.Windows.Data;
using Toxy.Managers;

namespace Toxy.Converters.FileTransfers
{
    public class TransferStateToTopButtonStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Styles.xaml");

            var inProgress = values[0] as bool?;
            var direction = values[1] as FileTransferDirection?;
            
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
