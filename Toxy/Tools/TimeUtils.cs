using System;
using System.Runtime.InteropServices;

namespace Toxy.Tools
{
    public class TimeUtils
    {
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);

            if (!GetLastInputInfo(ref lastInput))
                throw new Exception(GetLastError().ToString());

            var timespan = TimeSpan.FromMilliseconds(Environment.TickCount - lastInput.dwTime);
            return timespan;
        }
    }
}
