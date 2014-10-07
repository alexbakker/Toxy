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

        public const UInt32 SND_ASYNC = 1;
        public const UInt32 SND_MEMORY = 4;
        [DllImport("Winmm.dll")]
        public static extern bool PlaySound(byte[] data, IntPtr hMod, UInt32 dwFlags);

        public static void PlayWavResource(string wav)
        {
            string strNameSpace =
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.ToString();

            using (Stream str = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(strNameSpace + "." + wav))
            {
                if (str == null)
                    return;
                byte[] bStr = new Byte[str.Length];
                str.Read(bStr, 0, (int)str.Length);
                PlaySound(bStr, IntPtr.Zero, SND_ASYNC | SND_MEMORY);
            }
        }
    }
}
