using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Toxy.Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            var updateManager = new UpdateManager();
            updateManager.ProcessArguments(args);

            string currentVersion = updateManager.GetCurrentVersion();
            if (updateManager.ForceNightly)
            {
                updateManager.RunUpdate(updateManager.NightlyUri);
            }
            else if (updateManager.ForceUpdate)
            {
                var latest = updateManager.GetLatestVersion();
                updateManager.RunUpdate(Tools.IsX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
            }
            else if (string.IsNullOrEmpty(currentVersion))
            {
                var result = MessageBox.Show("Could not find Toxy in this directory. Do you want to download the latest version?", "Toxy not found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    var latest = updateManager.GetLatestVersion();
                    updateManager.RunUpdate(Tools.IsX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
                }
            }
            else
            {
                var info = updateManager.GetVersionInfo();
                if (info != null)
                {
                    var latest = updateManager.GetLatestVersion();
                    if (new Version(currentVersion) < new Version((string)latest["version"]))
                        updateManager.RunUpdate(Tools.IsX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
                    else
                    {
                        if (File.Exists(Path.Combine(updateManager.Dir, "Toxy.exe")))
                            Process.Start(Path.Combine(updateManager.Dir, "Toxy.exe"));
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(updateManager.Dir, "Toxy.exe")))
                        Process.Start(Path.Combine(updateManager.Dir, "Toxy.exe"));
                }
            }
        }
    }
}
