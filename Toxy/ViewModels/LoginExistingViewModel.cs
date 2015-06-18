using System.Collections.ObjectModel;
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

        public ObservableCollection<ProfileInfo> Profiles
        {
            get { return new ObservableCollection<ProfileInfo>(ProfileManager.GetAllProfiles()); }
        }

        public event MouseButtonEventHandler OnLoginButtonClicked;

        public void RaiseButtonClicked(object sender, MouseButtonEventArgs e)
        {
            if (OnLoginButtonClicked != null)
                OnLoginButtonClicked(sender, e);
        }
    }
}
