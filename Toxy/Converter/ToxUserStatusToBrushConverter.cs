using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using SharpTox.Core;
using Toxy.Extenstions;

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
            ToxUserStatusNONE = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Online.ToBitmapImage(ImageFormat.Bmp)
            };
            ToxUserStatusAWAY = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Away.ToBitmapImage(ImageFormat.Bmp)
            };
            ToxUserStatusBUSY = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Busy.ToBitmapImage(ImageFormat.Bmp)
            };
            ToxUserStatusINVALID = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Offline.ToBitmapImage(ImageFormat.Bmp)
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