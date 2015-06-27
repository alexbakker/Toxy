using System;
using SharpTox.Core;
using Toxy.MVVM;
using System.Windows.Media;
using System.Windows;
using SharpTox.Av;
using Toxy.Managers;

namespace Toxy.ViewModels
{
    public class FriendControlViewModel : ViewModelBase, IChatObject
    {
        public FriendControlViewModel()
        {
            _conversationView = new ConversationViewModel(this);
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

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (Equals(value, _isSelected))
                {
                    return;
                }
                _isSelected = value;
                OnPropertyChanged(() => IsSelected);
            }
        }

        private ConversationViewModel _conversationView;
        public ConversationViewModel ConversationView
        {
            get { return _conversationView; }
            set
            {
                if (Equals(value, _conversationView))
                {
                    return;
                }
                _conversationView = value;
                OnPropertyChanged(() => ConversationView);
            }
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get { return _isOnline; }
            set
            {
                if (Equals(value, _isOnline))
                {
                    return;
                }
                _isOnline = value;
                OnPropertyChanged(() => IsOnline);
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

        private bool _hasUnreadMessages;
        public bool HasUnreadMessages
        {
            get { return _hasUnreadMessages; }
            set
            {
                if (Equals(value, _hasUnreadMessages))
                {
                    return;
                }
                _hasUnreadMessages = value;
                OnPropertyChanged(() => HasUnreadMessages);
            }
        }

        private CallState _callState;
        public CallState CallState
        {
            get { return _callState; }
            set
            {
                if (Equals(value, _callState))
                {
                    return;
                }

                //TODO: tidy up
                //if (value.HasFlag(CallState.SendingVideo) != _callState.HasFlag(CallState.SendingVideo))
                //    IsInVideoCall = value.HasFlag(CallState.SendingVideo);

                if (value.HasFlag(CallState.SendingVideo) != _callState.HasFlag(CallState.SendingVideo))
                    IsReceivingVideo = value.HasFlag(CallState.SendingVideo);

                _callState = value;
                OnPropertyChanged(() => CallState);
            }
        }

        private bool _isInVideoCall;
        public bool IsInVideoCall
        {
            get { return _isInVideoCall; }
            set
            {
                if (Equals(value, _isInVideoCall))
                {
                    return;
                }
                _isInVideoCall = value;
                OnPropertyChanged(() => IsInVideoCall);
            }
        }

        private bool _isReceivingVideo;
        public bool IsReceivingVideo
        {
            get { return _isReceivingVideo; }
            set
            {
                if (Equals(value, _isReceivingVideo))
                {
                    return;
                }
                _isReceivingVideo = value;
                OnPropertyChanged(() => IsReceivingVideo);
            }
        }
    }
}
