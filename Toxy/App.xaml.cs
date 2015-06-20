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
            Debugging.Write("Tox version: " + ToxVersion.Current.ToString());
            Config.Instance.Reload();

            if (string.IsNullOrEmpty(Config.Instance.ProfilePath))
            {
                new LoginWindow().Show();
            }
            else
            {
                Debugging.Write("Skipping login screen");
                ProfileManager.Instance.SwitchTo(new ProfileInfo(Config.Instance.ProfilePath));
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Config.Instance.Save();

            ProfileManager.Instance.Save();
            ProfileManager.Instance.Dispose();
        }
    }
}
