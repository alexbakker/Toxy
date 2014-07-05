using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro;
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

    public class MainWindowViewModel : ViewModelBase
    {

        public MainWindowViewModel()
        {
            var chatObjects = new ObservableCollection<IChatObject>();
            // notify the GroupChatCollection property to (used for menu items)
            chatObjects.CollectionChanged += (sender, args) => this.OnPropertyChanged(() => this.GroupChatCollection);
            this.ChatCollection = chatObjects;
            this.ChatRequestCollection = chatObjects;

            // create accent color menu items for the demo
            this.AccentColors = ThemeManager.Accents
                                            .Select(a => new AccentColorMenuData() { Name = a.Name, ColorBrush = a.Resources["AccentColorBrush"] as Brush })
                                            .ToList();

            this.AppThemes = ThemeManager.AppThemes
                                          .Select(a => new AppThemeMenuData() { Name = a.Name, BorderColorBrush = a.Resources["BlackColorBrush"] as Brush, ColorBrush = a.Resources["WhiteColorBrush"] as Brush })
                                          .ToList();
        }

        public List<AccentColorMenuData> AccentColors { get; set; }
        public List<AppThemeMenuData> AppThemes { get; set; }

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

        public bool IsFriendSelected
        {
            get { return this.SelectedChatObject is IFriendObject; }
        }

        public bool IsGroupSelected
        {
            get { return this.SelectedChatObject is IGroupObject; }
        }

        public int SelectedChatNumber
        {
            get
            {
                var chatObject = this.SelectedChatObject;
                return chatObject != null ? chatObject.ChatNumber : -1;
            }
        }

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
    }
}
