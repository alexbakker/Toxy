using System.Collections.ObjectModel;
using Toxy.Managers;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class LoginWindowViewModel : ViewModelBase
    {
        private bool _isLoginExistingSelected = true;
        public bool IsLoginExistingSelected
        {
            get { return _isLoginExistingSelected; }
            set
            {
                if (Equals(value, _isLoginExistingSelected))
                {
                    return;
                }
                _isLoginExistingSelected = value;
                OnPropertyChanged(() => IsLoginExistingSelected);
                OnPropertyChanged(() => IsLoginNewSelected);
            }
        }

        public bool IsLoginNewSelected
        {
            get { return !_isLoginExistingSelected; }
        }

        public ProfileInfo SelectedProfile { get; set; }
        public string ProfileName { get; set; }
        public string Password { get; set; }
        public bool RememberChoice { get; set; }

        public ObservableCollection<ProfileInfo> Profiles
        {
            get { return new ObservableCollection<ProfileInfo>(ProfileManager.GetAllProfiles()); }
        }
    }
}
