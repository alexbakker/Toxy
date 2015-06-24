using SharpTox.Av;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Toxy.Tools
{
    public static class VideoUtils
    {
        [DllImport("Toxy.Tools.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "yuv420tobgr")]
        private static extern void Yuv420ToBgr(ushort width, ushort height, byte[] y, byte[] u, byte[] v, uint yStride, uint uStride, uint vStride, byte[] output);

        [DllImport("Toxy.Tools.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "bgrtoyuv420")]
        private static extern void BgrToYuv420(byte[] planeY, byte[] planeU, byte[] planeV, byte[] rgb, ushort width, ushort height);

        public static BitmapSource ToxAvFrameToBitmap(ToxAvVideoFrame frame)
        {
            byte[] data = new byte[frame.Width * frame.Height * 4];
            Yuv420ToBgr((ushort)frame.Width, (ushort)frame.Height, frame.Y, frame.U, frame.V, (uint)frame.YStride, (uint)frame.UStride, (uint)frame.VStride, data);

            int bytesPerPixel = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            int stride = 4 * ((frame.Width * bytesPerPixel + 3) / 4);

            var source = BitmapSource.Create(frame.Width, frame.Height, 96d, 96d, PixelFormats.Bgra32, null, data, stride);
            source.Freeze();

            return source;
        }
        
        public static ToxAvVideoFrame BitmapToToxAvFrame(Bitmap bmp)
        {
            var bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            byte[] bytes = new byte[bitmapData.Stride * bmp.Height];

            Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);

            byte[] y = new byte[bmp.Height * bmp.Width];
            byte[] u = new byte[(bmp.Height / 2) * (bmp.Width / 2)];
            byte[] v = new byte[(bmp.Height / 2) * (bmp.Width / 2)];

            BgrToYuv420(y, u, v, bytes, (ushort)bmp.Width, (ushort)bmp.Height);
            return new ToxAvVideoFrame(bmp.Width, bmp.Height, y, u, v);
        }
    }
}
