using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Toxy.Managers;
using Toxy.MVVM;
using Toxy.Windows;

namespace Toxy.ViewModels
{
    public class LoginExistingViewModel : ViewModelBase
    {
        public ProfileInfo SelectedProfile { get; set; }
        public string Password { get; set; }
        public bool RememberChoice { get; set; }

        public ObservableCollection<ProfileInfo> Profiles
        {
            get { return new ObservableCollection<ProfileInfo>(ProfileManager.GetAllProfiles()); }
        }

        public event RoutedEventHandler OnLoginButtonClicked;

        public void RaiseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (OnLoginButtonClicked != null)
                OnLoginButtonClicked(sender, e);
        }
    }
}
