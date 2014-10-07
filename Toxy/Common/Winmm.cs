using System;
using System.Runtime.InteropServices;
using System.Resources;
using System.IO;

namespace Win32
{
    public static class Winmm
    {
        public static void PlayMessageNotify()
        {
            PlayWavResource("Resources.Audio.Blop.wav");
        }

        private static byte[] resourceStreamLength;
        private const UInt32 SND_ASYNC = 1;
        private const UInt32 SND_MEMORY = 4;
        [DllImport("Winmm.dll")]
        private static extern bool PlaySound(byte[] data, IntPtr hMod, UInt32 dwFlags);

        private static void PlayWavResource(string wav)
        {
            var strNameSpace = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            if (resourceStreamLength == null)
            {
                var resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(strNameSpace + "." + wav);
                if (resourceStream != null)
                {
                    resourceStreamLength = new Byte[resourceStream.Length];
                    resourceStream.Read(resourceStreamLength, 0, (int)resourceStream.Length);
                }
            }

            if (resourceStreamLength != null)
                PlaySound(resourceStreamLength, IntPtr.Zero, SND_ASYNC | SND_MEMORY);
        }
    }
}
