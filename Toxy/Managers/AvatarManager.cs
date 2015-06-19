using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpTox.Core;
using Toxy.Extensions;
using System.Windows.Media.Imaging;

namespace Toxy.Managers
{
    public class AvatarManager
    {
        public static string AvatarDataPath = Path.Combine(ProfileManager.ProfileDataPath, "avatars");
        private Dictionary<int, byte[]> _avatars = new Dictionary<int, byte[]>();
        private byte[] _selfAvatar;

        private static AvatarManager _instance;
        public static AvatarManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AvatarManager();

                return _instance;
            }
        }

        private AvatarManager()
        {
            ProfileManager.Instance.Tox.OnFriendConnectionStatusChanged += Tox_OnFriendConnectionStatusChanged;
        }

        private void Tox_OnFriendConnectionStatusChanged(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            if (e.Status != ToxConnectionStatus.None)
                TransferManager.SendAvatar(e.FriendNumber, _selfAvatar);
        }

        public bool Contains(int friendNumber)
        {
            return _avatars.ContainsKey(friendNumber);
        }

        public string GetAvatarFilename(int friendNumber)
        {
            var publicKey = ProfileManager.Instance.Tox.GetFriendPublicKey(friendNumber);
            if (publicKey == null)
                return null;

            return Path.Combine(AvatarDataPath, publicKey.ToString() + ".png");
        }

        public string GetAvatarFilename(string publicKey)
        {
            return Path.Combine(AvatarDataPath, publicKey + ".png");

        }

        public bool HashMatches(int friendNumber, byte[] hash)
        {
            byte[] avatar = GetAvatar(friendNumber);
            if (avatar == null)
                return false;

            return ToxTools.Hash(avatar).SequenceEqual(hash);
        }

        public byte[] GetAvatar(int friendNumber)
        {
            if (_avatars.ContainsKey(friendNumber))
                return _avatars[friendNumber];

            return null;
        }

        public void Rehash()
        {
            _avatars.Clear();

            foreach (int friend in ProfileManager.Instance.Tox.Friends)
                Rehash(friend);

            LoadSelfAvatar();
        }

        //TODO: refactor this crap
        public void Rehash(int friendNumber)
        {
            if (_avatars.ContainsKey(friendNumber))
                _avatars.Remove(friendNumber);

            if (LoadAvatar(friendNumber) && _avatars[friendNumber].Length != 0)
            {
                //TODO: move this to the view model
                MainWindow.Instance.UInvoke(() =>
                {
                    var friend = MainWindow.Instance.ViewModel.CurrentFriendListView.ChatCollection.FirstOrDefault(f => f.ChatNumber == friendNumber);
                    if (friend == null)
                        return;

                    using (var stream = new MemoryStream(_avatars[friendNumber]))
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = stream;
                        bmp.EndInit();
                        bmp.Freeze();

                        friend.Avatar = bmp;
                    }
                });
            }
        }

        //TODO: refactor this crap
        private bool LoadAvatar(int friendNumber)
        {
            string filename = GetAvatarFilename(friendNumber);
            if (!File.Exists(filename))
                return false;

            try
            {
                byte[] avatar = File.ReadAllBytes(filename);
                _avatars.Add(friendNumber, avatar);
                return true;
            }
            catch (Exception ex)
            {
                Debugging.Write("Failed to load avatar from disk: " + ex.ToString());
                return false;
            }
        }

        //TODO: refactor this crap too
        private bool LoadSelfAvatar()
        {
            string filename = Path.Combine(AvatarDataPath, ProfileManager.Instance.Tox.Id.PublicKey.ToString() + ".png");
            if (!File.Exists(filename))
                return false;

            try
            {
                _selfAvatar = File.ReadAllBytes(filename);
                //TODO: move this to the view model
                MainWindow.Instance.UInvoke(() =>
                {
                    using (var stream = new MemoryStream(_selfAvatar))
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = stream;
                        bmp.EndInit();
                        bmp.Freeze();

                        MainWindow.Instance.ViewModel.CurrentSelfView.Avatar = bmp;
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                Debugging.Write("Failed to load avatar from disk: " + ex.ToString());
                return false;
            }
        }

        public void SaveAvatar(string publicKey, byte[] bytes)
        {
            try
            {
                File.WriteAllBytes(GetAvatarFilename(publicKey), bytes);
            }
            catch { }
        }

        public void RemoveSelfAvatar()
        {
            try
            {
                _selfAvatar = null;
                string filename = GetAvatarFilename(ProfileManager.Instance.Tox.Id.PublicKey.ToString());

                if (File.Exists(filename))
                    File.Delete(filename);

                foreach(int friend in ProfileManager.Instance.Tox.Friends)
                    if (ProfileManager.Instance.Tox.IsFriendOnline(friend))
                        TransferManager.SendAvatar(friend, null);
            }
            catch { }
        }

        public void RemoveAvatar(int friendNumber)
        {
            try
            {
                if (_avatars.ContainsKey(friendNumber))
                    _avatars.Remove(friendNumber);

                string filename = GetAvatarFilename(ProfileManager.Instance.Tox.GetFriendPublicKey(friendNumber).ToString());

                if (File.Exists(filename))
                    File.Delete(filename);
            }
            catch { }
        }
    }
}
