using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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
