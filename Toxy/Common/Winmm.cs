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
            if (_soundData == null)
            {
                using (UnmanagedMemoryStream sound = Toxy.Properties.Resources.Blop)
                {
                    _soundData = new byte[sound.Length];
                    sound.Read(_soundData, 0, (int)sound.Length);
                }
            }
            PlaySound(_soundData, IntPtr.Zero, SND_ASYNC | SND_MEMORY);
        }

        private static byte[] _soundData;
        private const UInt32 SND_ASYNC = 1;
        private const UInt32 SND_MEMORY = 4;
        [DllImport("Winmm.dll")]
        private static extern bool PlaySound(byte[] data, IntPtr hMod, UInt32 dwFlags);
    }
}
