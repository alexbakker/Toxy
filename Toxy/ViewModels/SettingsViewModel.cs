using System;
using System.Linq;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

using Toxy.Misc.QR;
using Toxy.Managers;
using Toxy.MVVM;
using NAudio.Wave;
using Toxy.Views;
using Toxy.Tools;

namespace Toxy.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public ObservableCollection<DeviceInfo> RecordingDevices { get; set; }
        public ObservableCollection<DeviceInfo> PlaybackDevices { get; set; }

        private DeviceInfo _selectedRecordingDevice;
        public DeviceInfo SelectedRecordingDevice
        {
            get { return _selectedRecordingDevice; }
            set
            {
                _selectedRecordingDevice = value;
                Config.Instance.RecordingDevice = _selectedRecordingDevice;
            }
        }

        private DeviceInfo _selectedPlaybackDevice;
        public DeviceInfo SelectedPlaybackDevice
        {
            get { return _selectedPlaybackDevice; }
            set
            {
                _selectedPlaybackDevice = value;
                Config.Instance.PlaybackDevice = _selectedPlaybackDevice;
            }
        }

        public ObservableCollection<ProfileInfo> Profiles
        {
            get
            {
                return new ObservableCollection<ProfileInfo>(ProfileManager.GetAllProfiles());
            }
        }

        private ProfileViewModel _currentProfileView;
        public ProfileViewModel CurrentProfileView
        {
            get { return _currentProfileView; }
            set
            {
                if (Equals(value, _currentProfileView))
                {
                    return;
                }
                _currentProfileView = value;
                OnPropertyChanged(() => CurrentProfileView);
            }
        }

        private ProfileInfo _selectedProfile;
        public ProfileInfo SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                if (Equals(value, _selectedProfile))
                {
                    return;
                }
                _selectedProfile = value;

                var profile = ToxSave.FromDisk(_selectedProfile.Path);
                if (profile != null)
                    CurrentProfileView = new ProfileViewModel(profile);

                OnPropertyChanged(() => SelectedProfile);
            }
        }

        public string ToxID { get { return App.Tox.Id.ToString(); } }

        public ImageSource QRCode { get; private set; }

        public MainWindowViewModel MainViewModel { get; private set; }

        public SettingsViewModel(MainWindowViewModel mainModel)
        {
            MainViewModel = mainModel;

            RecordingDevices = new ObservableCollection<DeviceInfo>();
            PlaybackDevices = new ObservableCollection<DeviceInfo>();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var capabilities = WaveIn.GetCapabilities(i);
                RecordingDevices.Add(new DeviceInfo() { Number = i, Name = capabilities.ProductName });
            }

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                PlaybackDevices.Add(new DeviceInfo() { Number = i, Name = capabilities.ProductName });
            }

            //generate the qr code of our tox id beforehand
            var generator = new QRCodeGenerator();
            var qrCode = generator.CreateQrCode(App.Tox.Id.ToString(), QRCodeGenerator.ECCLevel.H);

            //TODO: edit the source of QRCoder to directly create a BitmapSource so that we don't have to convert it
            using (var bitmap = qrCode.GetGraphic(20))
            {
                var ptr = IntPtr.Zero;
                try
                {
                    ptr = bitmap.GetHbitmap();
                    QRCode = Imaging.CreateBitmapSourceFromHBitmap(ptr, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        DeleteObject(ptr);
                }
            }
        }

        public void AddProfile(ProfileInfo info)
        {
            Profiles.Add(info);
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ptr);
    }

    public class DeviceInfo
    {
        public int Number { get; set; }
        public string Name { get; set; }
    }

    public class ProfileInfo
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public string FileName { get; private set; }

        public ProfileInfo(string path)
        {
            Path = path;
            Name = path.Split(new[] { "\\" }, StringSplitOptions.None).Last().Replace(".tox", "");
            FileName = Name + ".tox";
        }
    }
}
