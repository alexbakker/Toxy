using System;
using System.Windows.Controls;

namespace Toxy.Extensions
{
    public static class ControlExtensions
    {
        public static void UInvoke(this Control control, Action action)
        {
            control.Dispatcher.BeginInvoke(action);
        }
    }
}
