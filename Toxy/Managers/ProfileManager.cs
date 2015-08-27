using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Toxy.ViewModels;
using SharpTox.Core;
using SharpTox.Av;

namespace Toxy.Managers
{
    public class ProfileManager : IDisposable
    {
        public static string ProfileDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tox");

        private List<IToxManager> _managers { get; set; } = new List<IToxManager>();

        private static ProfileManager _instance;
        public static ProfileManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProfileManager();

                return _instance;
            }
        }

        public TransferManager TransferManager { get; private set; }
        public AvatarManager AvatarManager { get; private set; }
        public CallManager CallManager { get; private set; }
        public ConnectionManager ConnectionManager { get; private set; }

        public Tox Tox { get; private set; }
        public ToxAv ToxAv { get; private set; }
        public ProfileInfo CurrentProfile { get; private set; }

        public ProfileInfo CreateNew(string profileName)
        {
            string path = Path.Combine(ProfileDataPath, profileName + ".tox");
            if (File.Exists(path))
                return null;

            var tox = new Tox(ToxOptions.Default);
            tox.Name = profileName;
            tox.StatusMessage = "Toxing on Toxy";

            if (!tox.GetData().Save(path))
                return null;

            tox.Dispose();
            return new ProfileInfo(path);
        }

        public void SwitchTo(ProfileInfo profile)
        {
            var options = ToxOptions.Default;
            options.Ipv6Enabled = Config.Instance.EnableIpv6;

            if (Config.Instance.ProxyType != ToxProxyType.None)
            {
                options.UdpEnabled = false;
                options.ProxyType = Config.Instance.ProxyType;
                options.ProxyHost = Config.Instance.ProxyAddress;
                options.ProxyPort = Config.Instance.ProxyPort;
            }
            else
            {
                options.UdpEnabled = Config.Instance.EnableUdp;
            }

            Tox newTox;

            if (profile != null)
            {
                var data = ToxData.FromDisk(profile.Path);
                if (data == null)
                    throw new Exception("Could not load profile.");

                //why does this always return true?!?
                //if (data.IsEncrypted)
                //  throw new Exception("Data is encrypted, Toxy does not support encrypted profiles yet.");

                newTox = new Tox(options, data);
            }
            else
            {
                newTox = new Tox(options);
            }

            var newToxAv = new ToxAv(newTox);

            InitManagers(newTox, newToxAv);

            if (Tox != null)
                Tox.Dispose();

            if (ToxAv != null)
                ToxAv.Dispose();

            Tox = newTox;
            ToxAv = newToxAv;

            AvatarManager.Rehash();
            ConnectionManager.DoBootstrap();

            //TODO: move this someplace else and make it configurable
            if (string.IsNullOrEmpty(Tox.Name))
                Tox.Name = "Tox User";
            if (string.IsNullOrEmpty(Tox.StatusMessage))
                Tox.StatusMessage = "Toxing on Toxy";
            
            Tox.Start();
            ToxAv.Start();

            CurrentProfile = profile;
            MainWindow.Instance.Reload();
        }

        private void InitManagers(Tox tox, ToxAv toxAv)
        {
            if (TransferManager == null)
            {
                var transferManager = new TransferManager(tox);
                TransferManager = transferManager;
                _managers.Add(TransferManager);
            }
            else
            {
                TransferManager.SwitchProfile(tox, toxAv);
            }

            if (AvatarManager == null)
            {
                var avatarManager = new AvatarManager(tox);
                AvatarManager = avatarManager;
                _managers.Add(AvatarManager);
            }
            else
            {
                AvatarManager.SwitchProfile(tox, toxAv);
            }

            if (CallManager == null)
            {
                var callManager = new CallManager(tox, toxAv);
                CallManager = callManager;
                _managers.Add(CallManager);
            }
            else
            {
                CallManager.SwitchProfile(tox, toxAv);
            }

            if (ConnectionManager == null)
            {
                var connectionManager = new ConnectionManager(tox);
                ConnectionManager = connectionManager;
                _managers.Add(ConnectionManager);
            }
            else
            {
                ConnectionManager.SwitchProfile(tox, toxAv);
            }
        }

        public void Logout()
        {
            if (Tox != null)
                Tox.Dispose();

            if (ToxAv != null)
                ToxAv.Dispose();

            Config.Instance.ProfilePath = null;
            Config.Instance.Save();
        }

        public static IEnumerable<ProfileInfo> GetAllProfiles()
        {
            try
            {
                return Directory.GetFiles(ProfileDataPath, "*.tox", SearchOption.TopDirectoryOnly).
                        Where(s => s.EndsWith(".tox")).
                        Select(p => new ProfileInfo(p));
            }
            catch
            {
                return new List<ProfileInfo>();
            }
        }

        //should only be used when the application closes
        public void Dispose()
        {
            if (ToxAv != null)
                ToxAv.Dispose();

            if (Tox != null)
                Tox.Dispose();
        }

        public async Task SaveAsync()
        {
            if (CurrentProfile != null && Tox != null)
            {
                try
                {
                    Directory.CreateDirectory(ProfileDataPath);

                    using (var stream = new FileStream(CurrentProfile.Path, FileMode.Create))
                    {
                        byte[] data = Tox.GetData().Bytes;
                        await stream.WriteAsync(data, 0, data.Length);

                        Debugging.Write("Saved profile to disk");
                    }
                }
                catch (Exception ex) { Debugging.Write("Could not save profile: " + ex.ToString()); }
            }
            else
            {
                Debugging.Write("Tried to save profile but there is no profile in use!");
            }
        }

        public void Save()
        {
            if (CurrentProfile != null && Tox != null)
            {
                try
                {
                    Directory.CreateDirectory(ProfileDataPath);

                    if (Tox.GetData().Save(CurrentProfile.Path))
                        Debugging.Write("Saved profile to disk");
                    else
                        Debugging.Write("Could not save profile");
                }
                catch (Exception ex) { Debugging.Write("Could not save profile: " + ex.ToString()); }
            }
            else
            {
                Debugging.Write("Tried to save profile but there is no profile in use!");
            }
        }
    }
}
