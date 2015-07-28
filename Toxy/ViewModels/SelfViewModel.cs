using SharpTox.Core;
using System.Windows.Media;
using Toxy.Managers;
using Toxy.MVVM;
using Toxy.Extensions;
using System.Timers;
using Toxy.Tools;

namespace Toxy.ViewModels
{
    public class SelfViewModel : ViewModelBase
    {
        private Timer _awayTimer;

        public SelfViewModel()
        {
            ProfileManager.Instance.Tox.OnConnectionStatusChanged += Tox_OnConnectionStatusChanged;

            Name = ProfileManager.Instance.Tox.Name;
            StatusMessage = ProfileManager.Instance.Tox.StatusMessage;
            UserStatus = ProfileManager.Instance.Tox.Status;

            //TODO: only start a timer when EnableAutoAway changes to 'true' at runtime or if it's already 'true' here
            _awayTimer = new Timer(1000d);
            _awayTimer.Elapsed += awayTimer_Elapsed;
            _awayTimer.Start();
        }

        private void awayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Config.Instance.EnableAutoAway)
                IsAway = TimeUtils.GetIdleTime().TotalMinutes >= Config.Instance.AwayTimeMinutes;
        }

        //bad bad bad
        ~SelfViewModel()
        {
            if (_awayTimer != null)
                _awayTimer.Dispose();
        }

        private void Tox_OnConnectionStatusChanged(object sender, ToxEventArgs.ConnectionStatusEventArgs e)
        {
            MainWindow.Instance.UInvoke(() => ConnectionStatus = e.Status);
        }

        private bool _isAway;
        public bool IsAway
        {
            get { return _isAway; }
            set
            {
                if (Equals(value, _isAway))
                {
                    return;
                }

                //TODO: save the previous status and set it back to that instead of ToxUserStatus.None once the user isn't AFK anymore
                UserStatus = value ? ToxUserStatus.Away : ToxUserStatus.None;
                ProfileManager.Instance.Tox.Status = UserStatus;

                _isAway = value;
                OnPropertyChanged(() => Name);
            }
        }

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
                if (ProfileManager.Instance.Tox.Name != value)
                {
                    ProfileManager.Instance.Tox.Name = value;
                    ProfileManager.Instance.SaveAsync();
                }

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
                if (ProfileManager.Instance.Tox.StatusMessage != value)
                {
                    ProfileManager.Instance.Tox.StatusMessage = value;
                    ProfileManager.Instance.SaveAsync();
                }

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
