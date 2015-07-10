using SharpTox.Core;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class GroupControlViewModel : ViewModelBase, IGroupObject
    {
        public GroupControlViewModel()
        {
            _conversationView = new GroupConversationViewModel(this);
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

        private ObservableCollection<GroupPeer> _peers = new ObservableCollection<GroupPeer>();
        public ObservableCollection<GroupPeer> Peers
        {
            get { return _peers; }
            set
            {
                if (Equals(value, _peers))
                {
                    return;
                }
                _peers = value;
                OnPropertyChanged(() => Peers);
            }
        }

        public GroupPeer FindPeer(ToxKey key)
        {
            return _peers.FirstOrDefault(g => g.PublicKey == key);
        }
    }
}
