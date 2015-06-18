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
        //TODO: make this less accessible 
        public static Tox Tox { get; set; }
        public static ToxAv ToxAv { get; set; }

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
            System.IO.File.WriteAllBytes(ProfileManager.Instance.CurrentProfile.Path, Tox.GetData().Bytes);

            ToxAv.Dispose();
            Tox.Dispose();
        }
    }
}
