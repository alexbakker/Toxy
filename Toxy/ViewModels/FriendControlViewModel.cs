using System;
using SharpTox.Core;
using Toxy.MVVM;
using System.Windows.Media;
using System.Windows;
using SharpTox.Av;
using Toxy.Managers;
using System.Windows.Input;

namespace Toxy.ViewModels
{
    public class FriendControlViewModel : ViewModelBase, IFriendObject
    { 
        public FriendListViewModel FriendListView { get; private set; }

        public FriendControlViewModel(FriendListViewModel listModel)
        {
            FriendListView = listModel;
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

        private bool _isTyping;
        public bool IsTyping
        {
            get { return _isTyping; }
            set
            {
                if (Equals(value, _isTyping))
                {
                    return;
                }
                _isTyping = value;
                OnPropertyChanged(() => IsTyping);
            }
        }

        public bool SelfIsTyping { get; set; }

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

        private IConversationView _conversationView;
        public IConversationView ConversationView
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

        private ICommand _groupInviteCommand;

        public ICommand GroupInviteCommand
        {
            get
            {
                if (_groupInviteCommand == null)
                {
                    _groupInviteCommand = new DelegateCommand<IGroupObject>((g) =>
                    {
                        if (!ProfileManager.Instance.Tox.InviteFriend(ChatNumber, g.ChatNumber))
                            Debugging.Write(string.Format("Could not invite friend {0} to groupchat {1}", ChatNumber, g.ChatNumber));
                    },
                    (g) =>
                    {
                        return ConnectionStatus != ToxConnectionStatus.None;
                    });
                }

                return _groupInviteCommand;
            }
        }

        public bool EnableNotifications
        {
            get { return !Config.Instance.NotificationBlacklist.Contains(ProfileManager.Instance.Tox.GetFriendPublicKey(ChatNumber).ToString()); }
            set
            {
                string pubKey = ProfileManager.Instance.Tox.GetFriendPublicKey(ChatNumber).ToString();
                bool isInList = Config.Instance.NotificationBlacklist.Contains(pubKey);

                if (value == !isInList)
                    return;

                if (isInList && value)
                    Config.Instance.NotificationBlacklist.Remove(pubKey);
                else if (!isInList && !value)
                    Config.Instance.NotificationBlacklist.Add(pubKey);
            }
        }

        public void SetSelfTypingStatus(bool isTyping)
        {
            if (SelfIsTyping != isTyping)
            {
                var error = ToxErrorSetTyping.Ok;
                if (!ProfileManager.Instance.Tox.SetTypingStatus(ChatNumber, isTyping))
                    Debugging.Write(string.Format("Could not set typing status for friend {0}, error: {1}", ChatNumber, error));
                else
                    SelfIsTyping = isTyping;
            }
        }
    }
}
