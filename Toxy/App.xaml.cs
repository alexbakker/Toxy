using System;
using System.Reflection;
using System.Windows;

using SharpTox.Core;
using SharpTox.Av;

using Toxy.Managers;
using Toxy.Windows;
using Toxy.ViewModels;

namespace Toxy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //TODO: load config from appdata
            Debugging.Write("Tox version: " + ToxVersion.Current.ToString());
            new LoginWindow().Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await ProfileManager.Instance.SaveAsync();
            ProfileManager.Instance.Dispose();
        }
    }
}
