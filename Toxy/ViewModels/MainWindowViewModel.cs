using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using MahApps.Metro;
using Toxy.MVVM;
using NAudio.Wave;
using AForge.Video.DirectShow;

namespace Toxy.ViewModels
{
    public class AccentColorMenuData
    {
        public string Name { get; set; }
        public Brush BorderColorBrush { get; set; }
        public Brush ColorBrush { get; set; }

        protected virtual void DoChangeTheme(object sender)
        {
            var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var accent = ThemeManager.GetAccent(this.Name);
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, accent, theme.Item1);
        }
    }

    public class AppThemeMenuData : AccentColorMenuData
    {
        protected override void DoChangeTheme(object sender)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var appTheme = ThemeManager.GetAppTheme(this.Name);
            ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, appTheme);
        }
    }

    public class DeviceMenuData
    {
        public string Name { get; set; }
    }

    public class OutputDeviceMenuData : DeviceMenuData { }
    public class InputDeviceMenuData : DeviceMenuData { }
    public class VideoDeviceMenuData : DeviceMenuData { }

    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            this.MainToxyUser = new UserModel();

            UpdateChatCollection();
            this.ChatRequestCollection = new ObservableCollection<IChatObject>();

            // create accent color menu items for the demo
            this.AccentColors = ThemeManager.Accents
                                            .Select(a => new AccentColorMenuData() { Name = a.Name, ColorBrush = a.Resources["AccentColorBrush"] as Brush })
                                            .ToList();

            this.AppThemes = ThemeManager.AppThemes
                                          .Select(a => new AppThemeMenuData() { Name = a.Name, BorderColorBrush = a.Resources["BlackColorBrush"] as Brush, ColorBrush = a.Resources["WhiteColorBrush"] as Brush })
                                          .ToList();

            InputDevices = new ObservableCollection<InputDeviceMenuData>();
            OutputDevices = new ObservableCollection<OutputDeviceMenuData>();
            VideoDevices = new ObservableCollection<VideoDeviceMenuData>();
        }

        public List<AccentColorMenuData> AccentColors { get; set; }
        public List<AppThemeMenuData> AppThemes { get; set; }

        public ObservableCollection<OutputDeviceMenuData> OutputDevices { get; set; }
        public ObservableCollection<InputDeviceMenuData> InputDevices { get; set; }
        public ObservableCollection<VideoDeviceMenuData> VideoDevices { get; set; }

        public void UpdateDevices()
        {
            InputDevices.Clear();
            OutputDevices.Clear();
            VideoDevices.Clear();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
                InputDevices.Add(new InputDeviceMenuData { Name = WaveIn.GetCapabilities(i).ProductName });

            for (int i = 0; i < WaveOut.DeviceCount; i++)
                OutputDevices.Add(new OutputDeviceMenuData { Name = WaveOut.GetCapabilities(i).ProductName });

            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in videoDevices)
                VideoDevices.Add(new VideoDeviceMenuData { Name = device.Name });
        }

        private UserModel mainToxyUser;

        public UserModel MainToxyUser
        {
            get { return this.mainToxyUser; }
            set
            {
                if (Equals(value, this.mainToxyUser))
                {
                    return;
                }
                this.mainToxyUser = value;
                this.OnPropertyChanged(() => this.MainToxyUser);
            }
        }

        private ICollection<IChatObject> chatCollection;

        public ICollection<IChatObject> ChatCollection
        {
            get { return this.chatCollection; }
            set
            {
                if (Equals(value, this.chatCollection))
                {
                    return;
                }
                this.chatCollection = value;
                this.OnPropertyChanged(() => this.ChatCollection);
            }
        }

        public ICollection<IGroupObject> GroupChatCollection
        {
            get
            {
                return this.ChatCollection != null
                    ? this.ChatCollection.OfType<IGroupObject>().ToList()
                    : Enumerable.Empty<IGroupObject>().ToList();
            }
        }

        public bool AnyGroupsExists
        {
            get { return GroupChatCollection.Any(); }
        }

        private ICollection<IChatObject> chatRequestCollection;

        public ICollection<IChatObject> ChatRequestCollection
        {
            get { return this.chatRequestCollection; }
            set
            {
                if (Equals(value, this.chatRequestCollection))
                {
                    return;
                }
                this.chatRequestCollection = value;
                this.OnPropertyChanged(() => this.ChatRequestCollection);
            }
        }

        private IChatObject selectedChatObject;

        public IChatObject SelectedChatObject
        {
            get { return this.selectedChatObject; }
            set
            {
                if (Equals(value, this.selectedChatObject))
                {
                    return;
                }
                this.selectedChatObject = value;
                this.OnPropertyChanged(() => this.SelectedChatObject);
                this.OnPropertyChanged(() => this.IsFriendSelected);
                this.OnPropertyChanged(() => this.IsGroupSelected);
                this.OnPropertyChanged(() => this.SelectedChatNumber);
            }
        }

        private IFriendObject callingFriend;

        public IFriendObject CallingFriend
        {
            get { return this.callingFriend; }
            set
            {
                if (Equals(value, this.callingFriend))
                {
                    return;
                }
                this.callingFriend = value;
                this.OnPropertyChanged(() => this.CallingFriend);
            }
        }

        public bool IsFriendSelected
        {
            get { return this.SelectedChatObject is IFriendObject; }
        }

        public bool IsGroupSelected
        {
            get { return this.SelectedChatObject is IGroupObject; }
        }

        public Visibility BuildInfoVisibility
        {
            get { return string.IsNullOrEmpty(BuildInfo.BuildNumber) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public int SelectedChatNumber
        {
            get
            {
                var chatObject = this.SelectedChatObject;
                return chatObject != null ? chatObject.ChatNumber : -1;
            }
        }

        public bool HasNewMessage { get; set; }

        public IFriendObject GetFriendObjectByNumber(int friendnumber)
        {
            var fo = ChatCollection.OfType<IFriendObject>().FirstOrDefault(f => f.ChatNumber == friendnumber);
            return fo;
        }

        public IGroupObject GetGroupObjectByNumber(int groupnumber)
        {
            var go = ChatCollection.OfType<IGroupObject>().FirstOrDefault(f => f.ChatNumber == groupnumber);
            return go;
        }

        public void UpdateChatCollection(ObservableCollection<IChatObject> collection = null)
        {
            var chatObjects = collection == null ? new ObservableCollection<IChatObject>() : collection;
            // notify the GroupChatCollection property to (used for menu items)
            chatObjects.CollectionChanged += (sender, args) =>
            {
                this.OnPropertyChanged(() => this.GroupChatCollection);
                this.OnPropertyChanged(() => this.AnyGroupsExists);
            };
            this.ChatCollection = chatObjects;
        }
    }
}
