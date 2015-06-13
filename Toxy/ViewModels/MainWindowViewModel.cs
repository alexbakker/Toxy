using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AForge.Video.DirectShow;
using MahApps.Metro;
using NAudio.Wave;
using SharpTox.Core;
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
            if (File.Exists(configFilename))
            {
                config = ConfigTools.Load(configFilename);
            }
            else
            {
                config = new Config();
                ConfigTools.Save(config, configFilename);
            }
            ChangeLanguage(getShortLanguageName(config.Language));
            
            SpellcheckLanguages = Enum.GetNames(typeof(SpellcheckLanguage)).ToList();

            InputDevices = new ObservableCollection<InputMenuData>();
            OutputDevices = new ObservableCollection<OutputMenuData>();
            VideoDevices = new ObservableCollection<VideoMenuData>();

            AvatarStore = new AvatarStore(toxDataDir);
            
        }

        internal Config config;
        internal Tox tox;
        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageMenuData content = (LanguageMenuData)e.AddedItems[0];
            var fileShortCut = getShortLanguageName(content.Name);
            if (fileShortCut.Equals("fail"))
                return;

            ChangeLanguage(fileShortCut);
        }


        internal string getShortLanguageName(string LanguageDescriptor)
        {
            switch (LanguageDescriptor)
            {
                case "English":
                    return "Eng";

                case "Deutsch":
                    return "Ger";

                case "Dutch":
                    return "Nl";

                case "Pусский":
                    return "Ru";

                case "한글":
                    return "Kr";

            }

            return "fail";
        }

        internal string toxDataDir
        {
            get
            {
                if (!config.Portable)
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tox");
                else
                    return Environment.CurrentDirectory;
            }
        }

        internal string toxDataFilename
        {
            get
            {
                return Path.Combine(toxDataDir, string.Format("{0}.tox", string.IsNullOrEmpty(config.ProfileName) ? tox.Id.PublicKey.GetString().Substring(0, 10) : config.ProfileName));
            }
        }

        internal string toxOldDataFilename
        {
            get
            {
                return Path.Combine(toxDataDir, "tox_save");
            }
        }

        public string configFilename = "config.xml";

        internal string dbFilename
        {
            get
            {
                return Path.Combine(toxDataDir, "toxy.db");
            }
        }

        private void FillLanguages()
        {
            Languages.Add(new LanguageMenuData {Name = "English"});
            Languages.Add(new LanguageMenuData { Name = "Dutch" });
            Languages.Add(new LanguageMenuData { Name = "Deutsch" });
            Languages.Add(new LanguageMenuData { Name = "Pусский" });
            Languages.Add(new LanguageMenuData { Name = "한글" });
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

        private UserModel _mainToxyUser;

        public UserModel MainToxyUser
        {
            get { return _mainToxyUser; }
            set
            {
                if (Equals(value, _mainToxyUser))
                {
                    return;
                }
                _mainToxyUser = value;
                OnPropertyChanged(() => MainToxyUser);
            }
        }

        private ICollection<IChatObject> _chatCollection;

        public ICollection<IChatObject> ChatCollection
        {
            get { return _chatCollection; }
            set
            {
                if (Equals(value, _chatCollection))
                {
                    return;
                }
                _chatCollection = value;
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

        private ICollection<IChatObject> _chatRequestCollection;

        public ICollection<IChatObject> ChatRequestCollection
        {
            get { return _chatRequestCollection; }
            set
            {
                if (Equals(value, _chatRequestCollection))
                {
                    return;
                }
                _chatRequestCollection = value;
                OnPropertyChanged(() => ChatRequestCollection);
            }
        }

        private IChatObject _selectedChatObject;

        public IChatObject SelectedChatObject
        {
            get { return _selectedChatObject; }
            set
            {
                if (Equals(value, _selectedChatObject))
                {
                    return;
                }
                _selectedChatObject = value;
                OnPropertyChanged(() => SelectedChatObject);
                OnPropertyChanged(() => IsFriendSelected);
                OnPropertyChanged(() => IsGroupSelected);
                OnPropertyChanged(() => SelectedChatNumber);
            }
        }

        private IFriendObject _callingFriend;

        public IFriendObject CallingFriend
        {
            get { return _callingFriend; }
            set
            {
                if (Equals(value, _callingFriend))
                {
                    return;
                }
                _callingFriend = value;
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

	    private bool _spellcheckEnabled;

	    public bool SpellcheckEnabled
	    {
			get { return _spellcheckEnabled; }
		    set
		    {
			    if (Equals(value, _spellcheckEnabled))
			    {
				    return;
			    }
			    _spellcheckEnabled = value;
				OnPropertyChanged(() => SpellcheckEnabled);
		    }
	    }

	    private string _spellcheckLangCode;
        internal AvatarStore AvatarStore;

        public string SpellcheckLangCode
	    {
			get { return _spellcheckLangCode; }
		    set
		    {
			    if (Equals(value, _spellcheckLangCode))
			    {
				    return;
			    }
			    _spellcheckLangCode = value;
				OnPropertyChanged(() => SpellcheckLangCode);
		    }
	    }
    }
}
