using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Toxy.Extensions
{
    public static class WindowExtensions
    {
        public static void Flash(this Window window)
        {

        }

        public static void FixBackground(this Window window)
        {
            var handle = new WindowInteropHelper(window).EnsureHandle();
            var result = SetClassLong(handle, GCL_HBRBACKGROUND, GetSysColorBrush(COLOR_WINDOW));
        }

        private static IntPtr SetClassLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size > 4)
                return SetClassLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetClassLongPtr32(hWnd, nIndex, unchecked((uint)dwNewLong.ToInt32())));
        }

        private const int GCL_HBRBACKGROUND = -10;
        private const int COLOR_WINDOW = 5;

        [DllImport("user32.dll", EntryPoint = "SetClassLong")]
        private static extern uint SetClassLongPtr32(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
        private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSysColorBrush(int nIndex);
    }
}
