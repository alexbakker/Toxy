using System;
using System.Windows;
using System.Windows.Data;
using Toxy.Managers;

namespace Toxy.Converters.FileTransfers
{
    public class TransferStateToTopButtonVisualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dic = new ResourceDictionary();
            dic.Source = new Uri("pack://application:,,,/Toxy;component/Resources/Icons.xaml");

            var inProgress = values[0] as bool?;
            var direction = values[1] as FileTransferDirection?;

            if (inProgress == null || direction == null)
                return null;

            if (direction == FileTransferDirection.Incoming)
            {
                return inProgress == true ? dic["tox_no"] : dic["tox_check"];  
            }
            else if (direction == FileTransferDirection.Outgoing)
            {
                return dic["tox_no"];
            }

            //??
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
