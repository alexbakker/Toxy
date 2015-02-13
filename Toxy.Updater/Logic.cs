using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Toxy.Updater
{
    class Logic : IUpdateManager
    {
        public string Path = Environment.CurrentDirectory;
        static readonly string _updateFileName = "update.zip";
        static readonly string _publicKey = "3082010A0282010100F7E965CEDCC8B57C8849D68D0BA0E060A3E1EA952616D06A431B9D50D58F936ADEE71F7C15CC13C757F63766C750D180B4B868954D2C6A576CB27B9ECF4EE06FBEA54A8307973D56E468482966D6653B238BA681526AC01DDB5858FF0855896D9365E4007C0ED5A321E99F4210B3C976C383059E557056F0FA7311417273F04B8A207CDD8FF3DC89FDA505B337CE1C27BA46F176D5BA2B45BBAC35D06182909249BDEB835232B282B8F7FAB2C19720D90F1B3B677922C87DF503E86EADADC7C4CD63744F549279F9A4429CE4D745B0F36DDA2FA01107A1C142BE6C09AB58E631CC9756D831302044EACE9D71864E8566D95952A53880A64C875472F82E2251F90203010001";

        static readonly List<string> _extractedFiles = new List<string>();
        static readonly string _jsonUri = "http://toxy.content.impy.me/versions";

        private readonly UpdateParameterDescription updateParameterDescription;

        public event EventHandler DownloadAvaible;
        public event EventHandler StartDownloading;
        public event EventHandler ErrorOccurred;
        public event EventHandler DownloadStatusChanged;
        public event EventHandler Extracting;
        public event EventHandler Finish;

        public Logic(UpdateParameterDescription updateParamteterDescription)
        {
            updateParameterDescription = updateParamteterDescription;
        }



        public void Update()
        {
            string currentVersion = GetToxyVersion();
            if (updateParameterDescription.ForceNightly)
            {
                RunUpdate(NightlyUri);
            }
            else if (updateParameterDescription.ForceUpdate)
            {
                var latest = GetLatestVersion();
                RunUpdate(_isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
            }
            else if (string.IsNullOrEmpty(currentVersion))
            {
                OnDownloadAvaible();
            }
            else
            {
                var info = GetVersionInfo();
                if (info != null)
                {
                    var latest = GetLatestVersion();
                    if (new Version(currentVersion) < new Version((string)latest["version"]))
                        RunUpdate(_isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
                    else
                    {
                        OnFinish();
                        if (File.Exists(System.IO.Path.Combine(Path, "Toxy.exe")))
                            Process.Start(System.IO.Path.Combine(Path, "Toxy.exe"));
                    }
                }
                else
                {
                    OnFinish();
                    if (File.Exists(System.IO.Path.Combine(Path, "Toxy.exe")))
                        Process.Start(System.IO.Path.Combine(Path, "Toxy.exe"));
                }
            }
        }

        private bool _isX64
        {
            get
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
                    return true;
                else
                    return false;
            }
        }

        private string NightlyUri
        {
            get
            {
                if (_isX64)
                    return "https://jenkins.impy.me/job/Toxy%20x64/lastSuccessfulBuild/artifact/toxy_x64.zip";
                else
                    return "https://jenkins.impy.me/job/Toxy%20x86/lastSuccessfulBuild/artifact/toxy_x86.zip";
            }
        }

        private dynamic GetVersionInfo()
        {
            try
            {
                using (var client = new WebClient())
                    return JsonConvert.DeserializeObject<dynamic>(client.DownloadString(_jsonUri));
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Could not fetch update information:\n" + ex.Message);
                return null;
            }
        }

        private JToken GetLatestVersion()
        {
            return ((JArray)GetVersionInfo()).OrderBy(v => new Version((string)v["version"])).First();
        }

        private void RunUpdate(string uri)
        {
            OnStartDownloading(uri);
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
                OnErrorOccurred("Could not download update:\n" + ex.Message);
                OnFinish();
            }

            //while (!_dialog.HasUserCancelled && !_finished) { }
            //CleanUp(_dialog.HasUserCancelled);

            if (File.Exists(System.IO.Path.Combine(Path, "Toxy.exe")))
                Process.Start(System.IO.Path.Combine(Path, "Toxy.exe"));
        }

        

        public void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            OnExtracting();
            
            try
            {
                using (ZipFile file = ZipFile.Read(_updateFileName))
                {
                    foreach (ZipEntry entry in file)
                    {
                        // TODO
                        //_dialog.Line3 = entry.FileName;
                        entry.Extract(this.Path, ExtractExistingFileAction.OverwriteSilently);
                        _extractedFiles.Add(entry.FileName);
                    }

                    // TODO
                    //_dialog.Line2 = "Verifying signatures";

                    foreach (ZipEntry entry in file)
                    {
                        if (!entry.FileName.EndsWith(".dll") && !entry.FileName.EndsWith(".exe"))
                            continue;

                        // TODO
                        //_dialog.Line3 = entry.FileName;
                        
                        try
                        {
                            string status;
                            if (!VerifyCertificate(X509Certificate.CreateFromSignedFile(entry.FileName).GetRawCertData(), out status))
                            {
                                OnErrorOccurred(entry.FileName + " does not have a valid signature!\n\n" + status);
                                CleanUp(true);
                                break;
                            }
                        }
                        catch (CryptographicException ex)
                        {

                            OnErrorOccurred(string.Format("A cryptographic exception occurred while trying to verify the signature of: {0}\n{1}\n\nThis probably means that this file doesn't have a valid signature!", entry.FileName, ex.Message));
                            CleanUp(true);
                            break;
                        }
                        catch (Exception ex)
                        {
                            OnErrorOccurred(string.Format("An exception occurred while trying to verify the signature of: {0}\n{1}", entry.FileName, ex.Message));
                            CleanUp(true);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Could not extract update:\n" + ex.Message);
            }

            OnFinish();
        }

        private bool VerifyCertificate(byte[] certData, out string message)
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

        public void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadStatusChanged(e);
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

        private string GetToxyVersion()
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(System.IO.Path.Combine(Path, "Toxy.exe")).FileVersion;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected virtual void OnDownloadAvaible()
        {
            var handler = DownloadAvaible;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void StartDownload(object sender, EventArgs e)
        {
            var latest = GetLatestVersion();
            RunUpdate(_isX64 ? (string)latest["url_x64"] : (string)latest["url_x86"]);
        }

        protected virtual void OnStartDownloading(String uri)
        {
            var handler = StartDownloading;
            if (handler != null) handler(this, new UpdateStartEventArgs()
            {
                Uri =  uri
            });
        }

        protected virtual void OnErrorOccurred(String Error)
        {
            var handler = ErrorOccurred;
            if (handler != null) handler(this, new ErrorEventArgs()
            {
                ErrorMessage = Error
            });
        }

        protected virtual void OnDownloadStatusChanged(DownloadProgressChangedEventArgs e)
        {
            var handler = DownloadStatusChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnExtracting()
        {
            var handler = Extracting;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnFinish()
        {
            var handler = Finish;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
