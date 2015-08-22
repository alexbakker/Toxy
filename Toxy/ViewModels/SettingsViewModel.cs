using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using System.Windows.Media;

using Toxy.Misc.QR;
using Toxy.Managers;
using Toxy.MVVM;
using Toxy.Extensions;
using Toxy.Tools;

using NAudio.Wave;
using AForge.Video.DirectShow;
using SharpTox.Core;

namespace Toxy.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public ObservableCollection<DeviceInfo> RecordingDevices { get; set; }
        public ObservableCollection<DeviceInfo> PlaybackDevices { get; set; }
        public ObservableCollection<DeviceInfo> VideoDevices { get; set; }

        public AudioEngine AudioEngine { get; private set; }
        public VideoEngine VideoEngine { get; private set; }

        public DeviceInfo SelectedRecordingDevice
        {
            get { return Config.Instance.RecordingDevice; }
            set
            {
                Config.Instance.RecordingDevice = value;
                ReloadAudio();
            }
        }

        public DeviceInfo SelectedPlaybackDevice
        {
            get { return Config.Instance.PlaybackDevice; }
            set
            {
                Config.Instance.PlaybackDevice = value;
                ReloadVideo();
            }
        }

        public DeviceInfo SelectedVideoDevice
        {
            get { return Config.Instance.VideoDevice; }
            set
            {
                Config.Instance.VideoDevice = value;
                ReloadAudio();
            }
        }

        public bool SendTypingNotifications
        {
            get { return Config.Instance.SendTypingNotifications; }
            set { Config.Instance.SendTypingNotifications = value; }
        }

        public bool EnableAutoAway
        {
            get { return Config.Instance.EnableAutoAway; }
            set { Config.Instance.EnableAutoAway = value; }
        }

        public string AwayTimeMinutes
        {
            get { return Config.Instance.AwayTimeMinutes.ToString(); }
            set
            {
                int minutes;
                if (int.TryParse(value, out minutes))
                    Config.Instance.AwayTimeMinutes = minutes;
            }
        }

        public string ProxyAddress
        {
            get { return Config.Instance.ProxyAddress; }
            set { Config.Instance.ProxyAddress = value; }
        }

        public string ProxyPort
        {
            get { return Config.Instance.ProxyPort.ToString(); }
            set { Config.Instance.ProxyPort = string.IsNullOrEmpty(value) ? 0 : int.Parse(value); }
        }

        public int ProxyType
        {
            get { return (int)Config.Instance.ProxyType; }
            set { Config.Instance.ProxyType = (ToxProxyType)value; }
        }

        public bool EnableDeferredScrolling
        {
            get { return Config.Instance.EnableDeferredScrolling; }
            set { Config.Instance.EnableDeferredScrolling = value; }
        }

        private string _nospam;
        public string Nospam
        {
            get { return _nospam ?? ProfileManager.Instance.Tox.GetNospam().ToString(); }
            set
            {
                if (Equals(value, _nospam))
                {
                    return;
                }
                _nospam = value;
                OnPropertyChanged(() => Nospam);
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

        private ImageSource _currentVideoFrame;
        public ImageSource CurrentVideoFrame
        {
            get { return _currentVideoFrame; }
            set
            {
                if (Equals(value, _currentVideoFrame))
                {
                    return;
                }
                _currentVideoFrame = value;
                OnPropertyChanged(() => CurrentVideoFrame);
            }
        }

        private float _recordingVolume;
        public float RecordingVolume
        {
            get { return _recordingVolume; }
            set
            {
                if (Equals(value, _recordingVolume))
                {
                    return;
                }
                _recordingVolume = value;
                OnPropertyChanged(() => RecordingVolume);
            }
        }

        public string ToxID { get { return ProfileManager.Instance.Tox.Id.ToString(); } }

        public ImageSource QRCode { get; private set; }

        public MainWindowViewModel MainViewModel { get; private set; }

        public SettingsViewModel(MainWindowViewModel mainModel)
        {
            MainViewModel = mainModel;

            RecordingDevices = new ObservableCollection<DeviceInfo>();
            PlaybackDevices = new ObservableCollection<DeviceInfo>();
            VideoDevices = new ObservableCollection<DeviceInfo>();

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

            foreach (FilterInfo device in new FilterInfoCollection(FilterCategory.VideoInputDevice))
                VideoDevices.Add(new DeviceInfo() { Name = device.Name });

            //generate the qr code of our tox id beforehand
            var generator = new QRCodeGenerator();
            var qrCode = generator.CreateQrCode(ProfileManager.Instance.Tox.Id.ToString(), QRCodeGenerator.ECCLevel.H);

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

        public void ReloadAudio()
        {
            if (AudioEngine != null)
                AudioEngine.Dispose();

            AudioEngine = new AudioEngine();
            AudioEngine.OnMicVolumeChanged += AudioEngine_OnMicVolumeChanged;
            AudioEngine.StartRecording();
        }

        public void ReloadVideo()
        {
            if (VideoEngine != null)
                VideoEngine.Dispose();

            VideoEngine = new VideoEngine();
            VideoEngine.OnFrameAvailable += VideoEngine_OnFrameAvailable;
            VideoEngine.StartRecording();
        }

        private void AudioEngine_OnMicVolumeChanged(float volume)
        {
            MainWindow.Instance.UInvoke(() => RecordingVolume = volume * 100);
        }

        private void VideoEngine_OnFrameAvailable(Bitmap frame)
        {
            //TODO: move this whole process to an extension method
            //TODO: edit aforge source code to create a bitmapsource direcrtly instead of converting (?)
            using (var stream = new MemoryStream())
            {
                frame.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;
                frame.Dispose();

                var bitmapImg = new BitmapImage();
                bitmapImg.BeginInit();
                bitmapImg.StreamSource = stream;
                bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImg.EndInit();
                bitmapImg.Freeze();

                MainWindow.Instance.UInvoke(() => CurrentVideoFrame = bitmapImg);
            }
        }

        public void Kill()
        {
            if (AudioEngine != null)
            {
                AudioEngine.Dispose();
                AudioEngine = null;
            }

            if (VideoEngine != null)
            {
                VideoEngine.Dispose();
                VideoEngine = null;
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ptr);
    }

    [Serializable]
    public class DeviceInfo
    {
        public int Number { get; set; }
        public string Name { get; set; }

        public static bool operator ==(DeviceInfo info1, DeviceInfo info2)
        {
            if (object.ReferenceEquals(info1, info2))
                return true;

            if ((object)info1 == null ^ (object)info2 == null)
                return false;

            return (info1.Name == info2.Name);
        }

        public static bool operator !=(DeviceInfo node1, DeviceInfo node2)
        {
            return !(node1 == node2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            DeviceInfo info = obj as DeviceInfo;
            if ((object)info == null)
                return false;

            return this == info;
        }
    }

    [Serializable]
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
