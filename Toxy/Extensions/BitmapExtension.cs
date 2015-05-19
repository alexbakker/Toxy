using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Toxy.Extensions
{
    static class BitmapExtension
    {
        public static byte[] GetBytes(this Bitmap bmp)
        {
            var converter = new ImageConverter();
            byte[] bytes = (byte[])converter.ConvertTo(bmp, typeof(byte[]));
            return bytes;
        }

        public static BitmapImage ToBitmapImage(this Bitmap bmp, ImageFormat format)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, format);

                stream.Position = 0;

                BitmapImage newBmp = new BitmapImage();
                newBmp.BeginInit();
                newBmp.StreamSource = stream;
                newBmp.CacheOption = BitmapCacheOption.OnLoad;
                newBmp.EndInit();
                newBmp.Freeze();

                return newBmp;
            }
        }
    }
}
