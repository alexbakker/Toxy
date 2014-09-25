using System.Drawing;

namespace Toxy.Common
{
    static class BitmapExtension
    {
        public static byte[] GetBytes(this Bitmap bmp)
        {
            var converter = new ImageConverter();
            byte[] bytes = (byte[])converter.ConvertTo(bmp, typeof(byte[]));
            return bytes;
        }
    }
}
