using System;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class LoginWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentView = new LoginExistingViewModel();

        public ViewModelBase CurrentView
        {
            get { return _currentView; }
            set
            {
                if (Equals(value, _currentView))
                {
                    return;
                }

                _currentView = value;

                OnPropertyChanged(() => CurrentView);
                OnPropertyChanged(() => IsLoginExistingSelected);
                OnPropertyChanged(() => IsLoginNewSelected);
            }
        }

        public bool IsLoginExistingSelected
        {
            get
            {
                return CurrentView.GetType() == typeof(LoginExistingViewModel);
            }
        }

        public bool IsLoginNewSelected
        {
            get
            {
                return CurrentView.GetType() == typeof(LoginNewViewModel);
            }
        }
    }
}
