using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using SharpTox.Core;
using Toxy.Extenstions;
using Toxy.Common;

namespace Toxy.Converter
{
    public class ToxUserStatusToBrushConverter : IValueConverter
    {
        private Brush ToxStatusNONE;
        private Brush ToxStatusBUSY;
        private Brush ToxStatusAWAY;
        private Brush ToxStatusINVALID;

        public ToxUserStatusToBrushConverter()
        {
            ToxStatusNONE = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Online.ToBitmapImage(ImageFormat.Bmp)
            };
            ToxStatusAWAY = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Away.ToBitmapImage(ImageFormat.Bmp)
            };
            ToxStatusBUSY = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Busy.ToBitmapImage(ImageFormat.Bmp)
            };
            ToxStatusINVALID = new ImageBrush()
            {
                ImageSource = Toxy.Properties.Resources.Offline.ToBitmapImage(ImageFormat.Bmp)
            };
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToxStatus)
            {
                var status = (ToxStatus)value;
                switch (status)
                {
                    case ToxStatus.None:
                        return ToxStatusNONE;
                    case ToxStatus.Busy:
                        return ToxStatusBUSY;
                    case ToxStatus.Away:
                        return ToxStatusAWAY;
                    case ToxStatus.Invalid:
                        return ToxStatusINVALID;
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