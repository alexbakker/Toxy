using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using AForge.Video.DirectShow;
using MahApps.Metro;
using NAudio.Wave;
using Toxy.Common;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class AccentColorMenuData
    {
        public string Name { get; set; }
        public Brush BorderColorBrush { get; set; }
        public Brush ColorBrush { get; set; }

        protected virtual void DoChangeTheme(object sender)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var accent = ThemeManager.GetAccent(Name);
            ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);
        }
    }

    public class AppThemeMenuData : AccentColorMenuData
    {
        protected override void DoChangeTheme(object sender)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var appTheme = ThemeManager.GetAppTheme(Name);
            ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, appTheme);
        }
    }

    public class MenuData
    {
        public string Name { get; set; }
    }

    public class OutputMenuData : MenuData { }
    public class InputMenuData : MenuData { }
    public class VideoMenuData : MenuData { }

    public class LanguageMenuData : MenuData { }

    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            MainToxyUser = new UserModel();

            UpdateChatCollection();
            ChatRequestCollection = new ObservableCollection<IChatObject>();

            // create accent color menu items for the demo
            AccentColors = ThemeManager.Accents
                                            .Select(a => new AccentColorMenuData() { Name = a.Name, ColorBrush = a.Resources["AccentColorBrush"] as Brush })
                                            .ToList();

            AppThemes = ThemeManager.AppThemes
                                          .Select(a => new AppThemeMenuData() { Name = a.Name, BorderColorBrush = a.Resources["BlackColorBrush"] as Brush, ColorBrush = a.Resources["WhiteColorBrush"] as Brush })
                                          .ToList();

            Languages = new ObservableCollection<LanguageMenuData>();
            FillLanguages();

            this.SpellcheckLanguages = Enum.GetNames(typeof(SpellcheckLanguage)).ToList();

            InputDevices = new ObservableCollection<InputMenuData>();
            OutputDevices = new ObservableCollection<OutputMenuData>();
            VideoDevices = new ObservableCollection<VideoMenuData>();
        }

        private void FillLanguages()
        {
            Languages.Add(new LanguageMenuData {Name = "English"});
            Languages.Add(new LanguageMenuData { Name = "Dutch" });
            Languages.Add(new LanguageMenuData { Name = "Deutsch" });
            Languages.Add(new LanguageMenuData {Name = "Pусский" });
        }

        public List<AccentColorMenuData> AccentColors { get; set; }
        public List<AppThemeMenuData> AppThemes { get; set; }
		public List<string> SpellcheckLanguages { get; set; }

        public ObservableCollection<OutputMenuData> OutputDevices { get; set; }
        public ObservableCollection<InputMenuData> InputDevices { get; set; }
        public ObservableCollection<VideoMenuData> VideoDevices { get; set; }

        public  ObservableCollection<LanguageMenuData> Languages { get; set; }

        public void UpdateDevices()
        {
            InputDevices.Clear();
            OutputDevices.Clear();
            VideoDevices.Clear();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
                InputDevices.Add(new InputMenuData { Name = WaveIn.GetCapabilities(i).ProductName });

            for (int i = 0; i < WaveOut.DeviceCount; i++)
                OutputDevices.Add(new OutputMenuData { Name = WaveOut.GetCapabilities(i).ProductName });

            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in videoDevices)
                VideoDevices.Add(new VideoMenuData { Name = device.Name });
        }

        private UserModel mainToxyUser;

        public UserModel MainToxyUser
        {
            get { return mainToxyUser; }
            set
            {
                if (Equals(value, mainToxyUser))
                {
                    return;
                }
                mainToxyUser = value;
                OnPropertyChanged(() => MainToxyUser);
            }
        }

        private ICollection<IChatObject> chatCollection;

        public ICollection<IChatObject> ChatCollection
        {
            get { return chatCollection; }
            set
            {
                if (Equals(value, chatCollection))
                {
                    return;
                }
                chatCollection = value;
                OnPropertyChanged(() => ChatCollection);
            }
        }

        public ICollection<IGroupObject> GroupChatCollection
        {
            get
            {
                return ChatCollection != null
                    ? ChatCollection.OfType<IGroupObject>().ToList()
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
            get { return chatRequestCollection; }
            set
            {
                if (Equals(value, chatRequestCollection))
                {
                    return;
                }
                chatRequestCollection = value;
                OnPropertyChanged(() => ChatRequestCollection);
            }
        }

        private IChatObject selectedChatObject;

        public IChatObject SelectedChatObject
        {
            get { return selectedChatObject; }
            set
            {
                if (Equals(value, selectedChatObject))
                {
                    return;
                }
                selectedChatObject = value;
                OnPropertyChanged(() => SelectedChatObject);
                OnPropertyChanged(() => IsFriendSelected);
                OnPropertyChanged(() => IsGroupSelected);
                OnPropertyChanged(() => SelectedChatNumber);
            }
        }

        private IFriendObject callingFriend;

        public IFriendObject CallingFriend
        {
            get { return callingFriend; }
            set
            {
                if (Equals(value, callingFriend))
                {
                    return;
                }
                callingFriend = value;
                OnPropertyChanged(() => CallingFriend);
            }
        }

        public bool IsFriendSelected
        {
            get { return SelectedChatObject is IFriendObject; }
        }

        public bool IsGroupSelected
        {
            get { return SelectedChatObject is IGroupObject; }
        }

        public Visibility BuildInfoVisibility
        {
            get { return string.IsNullOrEmpty(BuildInfo.BuildNumber) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public int SelectedChatNumber
        {
            get
            {
                var chatObject = SelectedChatObject;
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
            var chatObjects = collection ?? new ObservableCollection<IChatObject>();
            // notify the GroupChatCollection property to (used for menu items)
            chatObjects.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged(() => GroupChatCollection);
                OnPropertyChanged(() => AnyGroupsExists);
            };
            ChatCollection = chatObjects;
        }



        public void ChangeLanguage(string fileShortCut)
        {
            // List all our resources      
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }

            var requestedCulture = string.Format("Languages/{0}.xaml", fileShortCut);
            ResourceDictionary resourceDictionary =
                dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            if (resourceDictionary == null)
            {
                requestedCulture = "Languages/Eng.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            }

            // If we have the requested resource, remove it from the list and place at the end.\      
            // Then this language will be our string table to use.      
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
        }

	    private bool spellcheckEnabled;

	    public bool SpellcheckEnabled
	    {
			get { return this.spellcheckEnabled; }
		    set
		    {
			    if (Equals(value, this.spellcheckEnabled))
			    {
				    return;
			    }
			    this.spellcheckEnabled = value;
				this.OnPropertyChanged(() => this.SpellcheckEnabled);
		    }
	    }

	    private string spellcheckLangCode;

	    public string SpellcheckLangCode
	    {
			get { return this.spellcheckLangCode; }
		    set
		    {
			    if (Equals(value, this.spellcheckLangCode))
			    {
				    return;
			    }
			    this.spellcheckLangCode = value;
				this.OnPropertyChanged(() => this.SpellcheckLangCode);
		    }
	    }
    }
}
