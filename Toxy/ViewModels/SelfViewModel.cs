using SharpTox.Core;
using System;
using System.Windows.Media;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class SelfViewModel : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (Equals(value, _name))
                {
                    return;
                }

                //TODO: check to make sure changing the status message was successful
                if (App.Tox.Name != value)
                    App.Tox.Name = value;

                _name = value;
                OnPropertyChanged(() => Name);
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                if (Equals(value, _statusMessage))
                {
                    return;
                }

                //TODO: check to make sure changing the status message was successful
                if (App.Tox.StatusMessage != value)
                    App.Tox.StatusMessage = value;

                _statusMessage = value;
                OnPropertyChanged(() => StatusMessage);
            }
        }

        public int ChatNumber { get; set; }

        private ToxUserStatus _userStatus;
        public ToxUserStatus UserStatus
        {
            get { return _userStatus; }
            set
            {
                if (Equals(value, _userStatus))
                {
                    return;
                }
                _userStatus = value;
                OnPropertyChanged(() => UserStatus);
            }
        }

        private ToxConnectionStatus _connectionStatus;
        public ToxConnectionStatus ConnectionStatus
        {
            get { return _connectionStatus; }
            set
            {
                if (Equals(value, _connectionStatus))
                {
                    return;
                }
                _connectionStatus = value;
                OnPropertyChanged(() => ConnectionStatus);
            }
        }

        private ImageSource _avatar;
        public ImageSource Avatar
        {
            get { return _avatar; }
            set
            {
                if (Equals(value, _avatar))
                {
                    return;
                }
                _avatar = value;
                OnPropertyChanged(() => Avatar);
            }
        }
    }
}
