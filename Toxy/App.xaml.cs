using System;
using System.Threading;
using System.Diagnostics;
using System.Windows;

namespace Toxy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine("Toxy crashed: " + e.Exception.ToString());
            e.Handled = false;
        }
    }
}
