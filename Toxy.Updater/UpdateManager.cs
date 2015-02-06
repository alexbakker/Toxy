using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Toxy.Updater
{
    class UpdateManager
    {
        private const string _updateFileName = "update.zip";
        private const string _jsonUri = "http://toxy.content.impy.me/versions";
        private const string _publicKey = "3082010A0282010100F7E965CEDCC8B57C8849D68D0BA0E060A3E1EA952616D06A431B9D50D58F936ADEE71F7C15CC13C757F63766C750D180B4B868954D2C6A576CB27B9ECF4EE06FBEA54A8307973D56E468482966D6653B238BA681526AC01DDB5858FF0855896D9365E4007C0ED5A321E99F4210B3C976C383059E557056F0FA7311417273F04B8A207CDD8FF3DC89FDA505B337CE1C27BA46F176D5BA2B45BBAC35D06182909249BDEB835232B282B8F7FAB2C19720D90F1B3B677922C87DF503E86EADADC7C4CD63744F549279F9A4429CE4D745B0F36DDA2FA01107A1C142BE6C09AB58E631CC9756D831302044EACE9D71864E8566D95952A53880A64C875472F82E2251F90203010001";
        
        private bool _finished = false;
        private List<string> _extractedFiles = new List<string>();

        private Win32ProgressDialog _dialog;
        public readonly string Dir = Environment.CurrentDirectory;

        public bool ForceNightly { get; private set; }
        public bool ForceUpdate { get; private set; }

        public string NightlyUri
        {
            get
            {
                if (Tools.IsX64)
                    return "https://jenkins.impy.me/job/Toxy%20x64/lastSuccessfulBuild/artifact/toxy_x64.zip";
                else
                    return "https://jenkins.impy.me/job/Toxy%20x86/lastSuccessfulBuild/artifact/toxy_x86.zip";
            }
        }

        public dynamic GetVersionInfo()
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

        public JToken GetLatestVersion()
        {
            return ((JArray)GetVersionInfo()).OrderBy(v => new Version((string)v["version"])).First();
        }

        public void RunUpdate(string uri)
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

            if (File.Exists(Path.Combine(Dir, "Toxy.exe")))
                Process.Start(Path.Combine(Dir, "Toxy.exe"));
        }

        public void ProcessArguments(string[] args)
        {
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "/force":
                    case "/f":
                        ForceUpdate = true;
                        break;
                    case "/nightly":
                        ForceNightly = true;
                        break;
                }
            }
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            _dialog.Line2 = "Extracting";

            try
            {
                using (ZipFile file = ZipFile.Read(_updateFileName))
                {
                    foreach (ZipEntry entry in file)
                    {
                        _dialog.Line3 = entry.FileName;
                        entry.Extract(Dir, ExtractExistingFileAction.OverwriteSilently);
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
                            if (!Tools.VerifyCertificate(X509Certificate.CreateFromSignedFile(entry.FileName).GetRawCertData(), _publicKey, out status))
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

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _dialog.SetProgress(e.ProgressPercentage);
        }

        private void CleanUp(bool clearFiles)
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

        public string GetCurrentVersion()
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(Path.Combine(Dir, "Toxy.exe")).FileVersion;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
