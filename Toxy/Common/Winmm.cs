using System;
using System.Runtime.InteropServices;
using System.Resources;
using System.IO;
using System.Media;

namespace Win32
{
    public static class Winmm
    {
        private static readonly SoundPlayer PhoneCallingPlayer = new SoundPlayer(ReadWavResource("Resources.Audio.PhoneCall1.wav"));

        public static void PlayMessageNotify()
        {
            PlayWavResource("Resources.Audio.Blop.wav");
        }

        public static void StartCallingNotify()
        {
            PhoneCallingPlayer.Play();
        }

        public static void StopCallingNotify()
        {
            PhoneCallingPlayer.Stop();
        }

        private static byte[] resourceStreamLength;
        private const UInt32 SND_ASYNC = 1;
        private const UInt32 SND_MEMORY = 4;
       
        [DllImport("Winmm.dll")]
        private static extern bool PlaySound(byte[] data, IntPtr hMod, UInt32 dwFlags);

        private static Stream ReadWavResource(string wav)
        {
            var strNameSpace = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            if (resourceStreamLength == null)
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(strNameSpace + "." + wav);
            }

            return null;
        }

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
