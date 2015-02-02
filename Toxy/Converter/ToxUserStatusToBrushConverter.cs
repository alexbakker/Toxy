using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpTox.Core;

namespace Toxy.Converter
{
    public class ToxUserStatusToBrushConverter : IValueConverter
    {
        private Brush ToxUserStatusNONE;
        private Brush ToxUserStatusBUSY;
        private Brush ToxUserStatusAWAY;
        private Brush ToxUserStatusINVALID;
        public ToxUserStatusToBrushConverter()
        {
            ToxUserStatusNONE = new ImageBrush(){
                ImageSource = new BitmapImage(new Uri(@"Resources\Icons\Online.png", UriKind.Relative))
            };
            ToxUserStatusAWAY = new ImageBrush()
            {
                ImageSource = new BitmapImage(new Uri(@"Resources\Icons\Away.png", UriKind.Relative))
            };
            ToxUserStatusBUSY = new ImageBrush()
            {
                ImageSource = new BitmapImage(new Uri(@"Resources\Icons\Busy.png", UriKind.Relative))
            };
            ToxUserStatusINVALID = new ImageBrush()
            {
                ImageSource = new BitmapImage(new Uri(@"Resources\Icons\Offline.png", UriKind.Relative))
            };
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToxUserStatus)
            {
                var status = (ToxUserStatus)value;
                switch (status)
                {
                    case ToxUserStatus.None:
                        return ToxUserStatusNONE;
                    case ToxUserStatus.Busy:
                        return ToxUserStatusBUSY;
                    case ToxUserStatus.Away:
                        return ToxUserStatusAWAY;
                    case ToxUserStatus.Invalid:
                        return ToxUserStatusINVALID;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}