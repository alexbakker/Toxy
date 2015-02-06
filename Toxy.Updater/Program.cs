using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZipFile = Ionic.Zip.ZipFile;

namespace Toxy.Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            var updateManger = new UpdateManager();
            updateManger.ProcessArguments(args);

            string currentVersion = updateManger.GetToxyVersion();
            if (updateManger._forceNightly)
            {
                updateManger.RunUpdate(updateManger._nightlyUri);
            }
            else if (updateManger._forceUpdate)
            {
                var latest = updateManger.GetLatestVersion();
                updateManger.RunUpdate(updateManger._isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
            }
            else if (string.IsNullOrEmpty(currentVersion))
            {
                var result = MessageBox.Show("Could not find Toxy in this directory. Do you want to download the latest version?", "Toxy not found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    var latest = updateManger.GetLatestVersion();
                    updateManger.RunUpdate(updateManger._isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
                }
            }
            else
            {
                var info = updateManger.GetVersionInfo();
                if (info != null)
                {
                    var latest = updateManger.GetLatestVersion();
                    if (new Version(currentVersion) < new Version((string)latest["version"]))
                        updateManger.RunUpdate(updateManger._isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
                    else
                    {
                        if (File.Exists(Path.Combine(updateManger._path, "Toxy.exe")))
                            Process.Start(Path.Combine(updateManger._path, "Toxy.exe"));
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(updateManger._path, "Toxy.exe")))
                        Process.Start(Path.Combine(updateManger._path, "Toxy.exe"));
                }
            }
        }

    }
}
