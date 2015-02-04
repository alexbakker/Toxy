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

namespace Toxy.Updater
{
    class Program
    {
        static Win32ProgressDialog _dialog;
        static string _path = Environment.CurrentDirectory;
        static string _updateFileName = "update.zip";
        static string _publicKey = "3082010A0282010100F7E965CEDCC8B57C8849D68D0BA0E060A3E1EA952616D06A431B9D50D58F936ADEE71F7C15CC13C757F63766C750D180B4B868954D2C6A576CB27B9ECF4EE06FBEA54A8307973D56E468482966D6653B238BA681526AC01DDB5858FF0855896D9365E4007C0ED5A321E99F4210B3C976C383059E557056F0FA7311417273F04B8A207CDD8FF3DC89FDA505B337CE1C27BA46F176D5BA2B45BBAC35D06182909249BDEB835232B282B8F7FAB2C19720D90F1B3B677922C87DF503E86EADADC7C4CD63744F549279F9A4429CE4D745B0F36DDA2FA01107A1C142BE6C09AB58E631CC9756D831302044EACE9D71864E8566D95952A53880A64C875472F82E2251F90203010001";

        static bool _forceUpdate = false;
        static bool _finished = false;

        static List<string> _extractedFiles = new List<string>();
        static string _jsonUri = "http://toxy.content.impy.me/versions";

        static bool isX64
        {
            get
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
                    return true;
                else
                    return false;
            }
        }

        static string nightlyUri
        {
            get
            {
                if (isX64)
                    return "https://jenkins.impy.me/job/Toxy%20x64/lastSuccessfulBuild/artifact/toxy_x64.zip";
                else
                    return "https://jenkins.impy.me/job/Toxy%20x86/lastSuccessfulBuild/artifact/toxy_x86.zip";
            }
        }

        static void Main(string[] args)
        {
            ProcessArguments(args);

            string currentVersion = GetToxyVersion();
            if (_forceUpdate)
            {
                var latest = GetLatestVersion();
                RunUpdate(isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
            }
            else if (string.IsNullOrEmpty(currentVersion))
            {
                var result = MessageBox.Show("Could not find Toxy in this directory. Do you want to download the latest version?", "Toxy not found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    var latest = GetLatestVersion();
                    RunUpdate(isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
                }
            }
            else
            {
                var info = GetVersionInfo();
                if (info != null)
                {
                    var latest = GetLatestVersion();
                    if (new Version(currentVersion) < new Version((string)latest["version"]))
                        RunUpdate(isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
                    else
                    {
                        if (File.Exists(Path.Combine(_path, "Toxy.exe")))
                            Process.Start(Path.Combine(_path, "Toxy.exe"));
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(_path, "Toxy.exe")))
                        Process.Start(Path.Combine(_path, "Toxy.exe"));
                }
            }
        }

        static dynamic GetVersionInfo()
        {
            try
            {
                using (var client = new WebClient())
                    return JsonConvert.DeserializeObject<dynamic>(client.DownloadString(_jsonUri));
            }
            catch (Exception ex)
            {
                ShowError("Could not fetch update information:\n" + ex.Message);
                return null;
            }
        }

        static JToken GetLatestVersion()
        {
            return ((JArray)GetVersionInfo()).OrderBy(v => new Version((string)v["version"])).First();
        }

        static void RunUpdate(string uri)
        {
            _dialog = new Win32ProgressDialog();
            _dialog.Title = "Toxy Updater";
            _dialog.Line1 = "Updating...";
            _dialog.Line2 = "Downloading update";
            _dialog.Line3 = uri;
            _dialog.ShowDialog(Win32ProgressDialog.PROGDLG.Normal);

            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += client_DownloadProgressChanged;
                    client.DownloadFileCompleted += client_DownloadFileCompleted;
                    client.DownloadFileAsync(new Uri(uri), _updateFileName, null);
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not download update:\n" + ex.Message);
                _finished = true;
            }

            while (!_dialog.HasUserCancelled && !_finished) { }
            CleanUp(_dialog.HasUserCancelled);

            if (File.Exists(Path.Combine(_path, "Toxy.exe")))
                Process.Start(Path.Combine(_path, "Toxy.exe"));
        }

        static void ProcessArguments(string[] args)
        {
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "/force":
                    case "/f":
                        _forceUpdate = true;
                        break;
                }
            }
        }

        static void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            _dialog.Line2 = "Extracting";

            try
            {
                using (ZipFile file = ZipFile.Read(_updateFileName))
                {
                    foreach (ZipEntry entry in file)
                    {
                        _dialog.Line3 = entry.FileName;
                        entry.Extract(_path, ExtractExistingFileAction.OverwriteSilently);
                        _extractedFiles.Add(entry.FileName);
                    }

                    _dialog.Line2 = "Verifying signatures";

                    foreach (ZipEntry entry in file)
                    {
                        if (!entry.FileName.EndsWith(".dll") && !entry.FileName.EndsWith(".exe"))
                            continue;

                        _dialog.Line3 = entry.FileName;

                        try
                        {
                            string status;
                            if (!VerifyCertificate(X509Certificate.CreateFromSignedFile(entry.FileName).GetRawCertData(), out status))
                            {
                                ShowError(entry.FileName + " does not have a valid signature!\n\n" + status);
                                CleanUp(true);
                                break;
                            }
                        }
                        catch (CryptographicException ex)
                        {
                            ShowError(string.Format("A cryptographic exception occurred while trying to verify the signature of: {0}\n{1}\n\nThis probably means that this file doesn't have a valid signature!", entry.FileName, ex.Message));
                            CleanUp(true);
                            break;
                        }
                        catch (Exception ex)
                        {
                            ShowError(string.Format("An exception occurred while trying to verify the signature of: {0}\n{1}", entry.FileName, ex.Message));
                            CleanUp(true);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not extract update:\n" + ex.Message);
            }

            _finished = true;
        }

        static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static bool VerifyCertificate(byte[] certData, out string message)
        {
            var chain = new X509Chain();

            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;

            var cert = new X509Certificate2(certData);
            bool success = chain.Build(cert);

            if (chain.ChainStatus.Count() > 0)
                message = string.Format("{0}\n{1}", chain.ChainStatus[0].Status, chain.ChainStatus[0].StatusInformation);
            else
                message = string.Empty;

            if (!success)
                return false;

            if (cert.GetPublicKeyString() != _publicKey)
            {
                message = "Public keys don't match";
                return false;
            }

            return true;
        }

        static void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _dialog.SetProgress(e.ProgressPercentage);
        }

        static void CleanUp(bool clearFiles)
        {
            try
            {
                if (File.Exists(_updateFileName))
                    File.Delete(_updateFileName);

                if (clearFiles)
                {
                    foreach (string file in _extractedFiles)
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                }
            }
            catch { }
        }

        static string GetToxyVersion()
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(Path.Combine(_path, "Toxy.exe")).FileVersion;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
