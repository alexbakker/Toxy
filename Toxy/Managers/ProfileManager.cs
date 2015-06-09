using System;
using System.IO;
using SharpTox.Core;
using Toxy.ViewModels;
using System.Reflection;
using SharpTox.Av;

namespace Toxy.Managers
{
    public class ProfileManager
    {
        public static string ProfileDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tox");

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

        public void SwitchTo(ProfileInfo profile)
        {
            Tox newTox;

            if (profile != null)
            {
                var data = ToxData.FromDisk(profile.Path);
                if (data == null)
                    throw new Exception("Could not load profile.");

                //why does this always return true?!?
                //if (data.IsEncrypted)
                //  throw new Exception("Data is encrypted, Toxy does not support encrypted profiles yet.");

                newTox = new Tox(ToxOptions.Default, data);
            }
            else
            {
                newTox = new Tox(ToxOptions.Default);
            }

            var newToxAv = new ToxAv(newTox);

            if (App.Tox != null)
                App.Tox.Dispose();

            if (App.ToxAv != null)
                App.ToxAv.Dispose();

            App.Tox = newTox;
            App.ToxAv = newToxAv;

            TransferManager.Get();
            CallManager.Get();
            AvatarManager.Instance.Rehash();
            ConnectionManager.Get().DoBootstrap();

            //TODO: move this someplace else and make it configurable
            if (string.IsNullOrEmpty(App.Tox.Name))
                App.Tox.Name = "Tox User";
            if (string.IsNullOrEmpty(App.Tox.StatusMessage))
                App.Tox.StatusMessage = "Toxing on Toxy";
            
            App.Tox.Start();
            App.ToxAv.Start();

            MainWindow.Instance.Reload();
        }

        /*private static void TransferSubscriptions<T>(T oldObject, T newObject)
        {
            //nice hacks, right?
            var events = typeof(T).GetEvents();

            foreach (var e in events)
            {
                string name = "_" + char.ToLower(e.Name[0]) + e.Name.Substring(1);

                var field = typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                var value = (MulticastDelegate)field.GetValue(oldObject);

                if (value != null)
                {
                    foreach (var handler in value.GetInvocationList())
                        e.AddEventHandler(newObject, handler);
                }
            }
        }*/
    }
}
