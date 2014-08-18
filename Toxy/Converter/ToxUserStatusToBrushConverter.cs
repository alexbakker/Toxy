using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
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
            ToxUserStatusNONE = new SolidColorBrush(Color.FromRgb(6, 225, 1));
            ToxUserStatusNONE.Freeze();
            ToxUserStatusBUSY = new SolidColorBrush(Color.FromRgb(214, 43, 79));
            ToxUserStatusBUSY.Freeze();
            ToxUserStatusAWAY = new SolidColorBrush(Color.FromRgb(229, 222, 31));
            ToxUserStatusAWAY.Freeze();
            ToxUserStatusINVALID = new SolidColorBrush(Colors.Red);
            ToxUserStatusINVALID.Freeze();
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