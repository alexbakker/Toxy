using System;
using System.Globalization;
using System.IO;
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
            ToxUserStatusNONE = new ImageBrush()
            {
                ImageSource = ImageSourceFromRessource(Toxy.Properties.Resources.Online)
            };
            ToxUserStatusAWAY = new ImageBrush()
            {
                ImageSource = ImageSourceFromRessource(Toxy.Properties.Resources.Away)
            };
            ToxUserStatusBUSY = new ImageBrush()
            {
                ImageSource = ImageSourceFromRessource(Toxy.Properties.Resources.Busy)
            };
            ToxUserStatusINVALID = new ImageBrush()
            {
                ImageSource = ImageSourceFromRessource(Toxy.Properties.Resources.Offline)
            };
        }

        private BitmapImage ImageSourceFromRessource(System.Drawing.Bitmap ressource)
        {
            var ms = new MemoryStream();
            ((System.Drawing.Bitmap)ressource).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            var image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image; 
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