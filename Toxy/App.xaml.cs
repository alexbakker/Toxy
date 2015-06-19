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

            //string toxProfilePath = System.IO.Path.Combine(ProfileManager.ProfileDataPath, "Impy.tox");
            //ProfileManager.Instance.SwitchTo(System.IO.File.Exists(toxProfilePath) ? new ProfileInfo(toxProfilePath) : null);

            new LoginWindow().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //TODO: move this to the profilemanager
            System.IO.File.WriteAllBytes(ProfileManager.Instance.CurrentProfile.Path, ProfileManager.Instance.Tox.GetData().Bytes);

            ProfileManager.Instance.Dispose();
        }
    }
}
