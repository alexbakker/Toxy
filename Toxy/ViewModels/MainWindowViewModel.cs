using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            _currentFriendListView = new FriendListViewModel(this);
            _currentSettingsView = new SettingsViewModel(this);
        }

        private ViewModelBase currentView;

        public ViewModelBase CurrentView
        {
            get { return currentView; }
            set
            {
                if (Equals(value, currentView))
                {
                    return;
                }

                if (currentView is SettingsViewModel)
                    (currentView as SettingsViewModel).Kill();

                currentView = value;
                OnPropertyChanged(() => CurrentView);
            }
        }

        private SelfViewModel _currentSelfView = new SelfViewModel();

        public SelfViewModel CurrentSelfView
        {
            get { return _currentSelfView; }
            set
            {
                if (Equals(value, _currentSelfView))
                {
                    return;
                }
                _currentSelfView = value;
                OnPropertyChanged(() => CurrentSelfView);
            }
        }

        private FriendListViewModel _currentFriendListView;
        public FriendListViewModel CurrentFriendListView
        {
            get { return _currentFriendListView; }
            set
            {
                if (Equals(value, _currentFriendListView))
                {
                    return;
                }
                _currentFriendListView = value;
                OnPropertyChanged(() => CurrentFriendListView);
            }
        }

        private SettingsViewModel _currentSettingsView;
        public SettingsViewModel CurrentSettingsView
        {
            get { return _currentSettingsView; }
            set
            {
                if (Equals(value, _currentSettingsView))
                {
                    return;
                }
                _currentSettingsView = value;
                OnPropertyChanged(() => CurrentSettingsView);
            }
        }
    }
}
