using System;
using System.Linq;
using System.Collections.ObjectModel;

using SharpTox.Core;
using Toxy.MVVM;
using Toxy.Managers;
using Toxy.Extensions;
using System.Windows.Documents;
using System.Collections.Generic;

namespace Toxy.ViewModels
{
    public class FriendListViewModel : ViewModelBase
    {
        public FriendListViewModel(MainWindowViewModel model)
        {
            MainWindowView = model;

            ProfileManager.Instance.Tox.OnFriendNameChanged += Tox_OnFriendNameChanged;
            ProfileManager.Instance.Tox.OnFriendStatusMessageChanged += Tox_OnFriendStatusMessageChanged;
            ProfileManager.Instance.Tox.OnFriendStatusChanged += Tox_OnFriendStatusChanged;
            ProfileManager.Instance.Tox.OnFriendConnectionStatusChanged += Tox_OnFriendConnectionStatusChanged;
            ProfileManager.Instance.Tox.OnFriendMessageReceived += Tox_OnFriendMessageReceived;
            ProfileManager.Instance.Tox.OnReadReceiptReceived += Tox_OnReadReceiptReceived;
            ProfileManager.Instance.Tox.OnFriendRequestReceived += Tox_OnFriendRequestReceived;
            ProfileManager.Instance.Tox.OnFriendTypingChanged += Tox_OnFriendTypingChanged;

            ProfileManager.Instance.Tox.OnGroupInvite += Tox_OnGroupInvite;
            ProfileManager.Instance.Tox.OnGroupTitleChanged += Tox_OnGroupTitleChanged;
            ProfileManager.Instance.Tox.OnGroupMessage += Tox_OnGroupMessage;
            ProfileManager.Instance.Tox.OnGroupAction += Tox_OnGroupAction;
            ProfileManager.Instance.Tox.OnGroupNamelistChange += Tox_OnGroupNamelistChange;

            Init();
        }

        private void Tox_OnGroupNamelistChange(object sender, ToxEventArgs.GroupNamelistChangeEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var group = FindGroup(e.GroupNumber);
                if (group == null)
                {
                    Debugging.Write("We don't know about this group!");
                    return;
                }

                var peer = group.FindPeer(ProfileManager.Instance.Tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));

                switch (e.Change)
                {
                    case ToxChatChange.PeerAdd:
                        {
                            if (peer != null)
                            {
                                Debugging.Write("Received ToxChatChange.PeerAdd but that peer is already in our list, replacing...");
                                group.Peers.Remove(peer);
                            }

                            peer = new GroupPeer(e.PeerNumber, ProfileManager.Instance.Tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
                            string name = ProfileManager.Instance.Tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber);

                            if (!string.IsNullOrEmpty(name))
                                peer.Name = name;

                            group.Peers.Add(peer);
                            break;
                        }
                    case ToxChatChange.PeerDel:
                        {
                            if (peer == null)
                                Debugging.Write("Received ToxChatChange.PeerDel but we don't know about this peer, ignoring...");
                            else
                                group.Peers.Remove(peer);

                            break;
                        }
                    case ToxChatChange.PeerName:
                        {
                            if (peer == null)
                                Debugging.Write("Received ToxChatChange.PeerName but we don't know about this peer, HELP!");
                            else
                                peer.Name = ProfileManager.Instance.Tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber);

                            break;
                        }
                }
            });
        }

        private void Tox_OnGroupAction(object sender, ToxEventArgs.GroupActionEventArgs e)
        {
            //TODO: handle separately
            Tox_OnGroupMessage(sender, new ToxEventArgs.GroupMessageEventArgs(e.GroupNumber, e.PeerNumber, e.Action));
        }

        private void Tox_OnGroupMessage(object sender, ToxEventArgs.GroupMessageEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var group = FindGroup(e.GroupNumber);
                if (group == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                var msg = new MessageViewModel(e.PeerNumber);
                msg.FriendName = ProfileManager.Instance.Tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber);
                msg.Message = e.Message;
                msg.Time = DateTime.Now.ToShortTimeString();

                (group.ConversationView as GroupConversationViewModel).AddMessage(msg);

                //if this is the first unread message, set HasUnreadMessages to true to make sure the status indicator gets updated
                if (!group.HasUnreadMessages && !group.IsSelected) group.HasUnreadMessages = true;
            });
        }

        private void Tox_OnGroupTitleChanged(object sender, ToxEventArgs.GroupTitleEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var group = FindGroup(e.GroupNumber);
                if (group == null)
                {
                    Debugging.Write("We don't know about this group!");
                    return;
                }

                group.Name = e.Title;
            });
        }

        private void Init()
        {
            foreach (int friend in ProfileManager.Instance.Tox.Friends)
            {
                string name = ProfileManager.Instance.Tox.GetFriendName(friend);
                string statusMessage = ProfileManager.Instance.Tox.GetFriendStatusMessage(friend);

                var model = new FriendControlViewModel(this);
                model.ChatNumber = friend;
                model.Name = string.IsNullOrEmpty(name) ? ProfileManager.Instance.Tox.GetFriendPublicKey(friend).ToString() : name;
                model.StatusMessage = statusMessage;

                AddObject(model);
            }

            RearrangeChatCollection();
        }

        private ObservableCollection<IChatObject> _chatCollection = new ObservableCollection<IChatObject>();

        public ObservableCollection<IChatObject> ChatCollection
        {
            get { return _chatCollection; }
            set
            {
                if (Equals(value, _chatCollection))
                {
                    return;
                }
                _chatCollection = value;
                OnPropertyChanged(() => ChatCollection);
            }
        }

        public ObservableCollection<IGroupObject> Groups
        {
            get { return new ObservableCollection<IGroupObject>(ChatCollection.OfType<IGroupObject>()); }
        }

        public bool AnyGroupExists
        {
            get { return ChatCollection.OfType<IGroupObject>().Count() != 0; }
        }

        private List<FriendRequest> _friendRequests = new List<FriendRequest>();
        private FriendRequest _currentFriendRequest;

        public FriendRequest CurrentFriendRequest
        {
            get { return _friendRequests.LastOrDefault(); }
        }

        public bool PendingFriendRequestsAvailable
        {
            get { return _friendRequests.Count != 0; }
        }

        public int PendingFriendRequestCount
        {
            get { return _friendRequests.Count; }
        }

        //we have to keep a reference of the main view model in order to change the current view from here
        public readonly MainWindowViewModel MainWindowView;

        private IChatObject _selectedChat;
        public IChatObject SelectedChat
        {
            get { return _selectedChat; }
            set
            {
                if (Equals(value, _selectedChat))
                {
                    return;
                }

                //TODO: make this a binding in xaml
                foreach (var chat in ChatCollection)
                    if (chat != value)
                        chat.IsSelected = false;

                _selectedChat = value;

                if (_selectedChat != null)
                {
                    if (_selectedChat.HasUnreadMessages) 
                        _selectedChat.HasUnreadMessages = false;

                    _selectedChat.IsSelected = true;
                    MainWindowView.CurrentView = (ViewModelBase)_selectedChat.ConversationView;
                }

                OnPropertyChanged(() => SelectedChat);
            }
        }

        public void AddObject(IChatObject obj)
        {
            ChatCollection.Add(obj);

            if (obj is IGroupObject)
            {
                OnPropertyChanged(() => Groups);
                OnPropertyChanged(() => AnyGroupExists);
            }
        }

        public bool RemoveObject(IChatObject obj)
        {
            var item = ChatCollection.FirstOrDefault();
            if (item != null)
                MainWindowView.CurrentView = (ViewModelBase)item.ConversationView;
            else
                MainWindowView.CurrentView = new AddFriendViewModel();

            bool success = ChatCollection.Remove(obj);
            if (success && obj is IGroupObject)
            {
                OnPropertyChanged(() => Groups);
                OnPropertyChanged(() => AnyGroupExists);
            }

            return success;
        }

        public void SortObject(IChatObject obj)
        {
            //this procedure may seem silly but will be better performance-wise compared to calling RearrangeChatCollection all day
            var tempColl = new ObservableCollection<IChatObject>(
                ChatCollection.OrderBy(chat =>
                    chat is IFriendObject ? GetStatusPriority(((IFriendObject)chat).IsOnline, ((IFriendObject)chat).UserStatus) : 0).
                    ThenBy(chat => chat.Name)
                );

            int oldIndex = ChatCollection.IndexOf(obj);
            int newIndex = tempColl.IndexOf(obj);

            if (oldIndex != newIndex)
                ChatCollection.Move(oldIndex, newIndex);
        }

        public void SelectObject(IChatObject obj)
        {
            SelectedChat = obj;
        }

        //this should only be used after initially populating the friend list
        public void RearrangeChatCollection()
        {
            //TODO: clean this up
            ChatCollection = new ObservableCollection<IChatObject>(
                ChatCollection.OrderBy(chat =>
                    chat is IFriendObject ? GetStatusPriority(((IFriendObject)chat).IsOnline, ((IFriendObject)chat).UserStatus) : 0).
                    ThenBy(chat => chat.Name)
                );
        }

        public void AcceptCurrentFriendRequest()
        {
            if (CurrentFriendRequest == null)
                return;

            var error = ToxErrorFriendAdd.Ok;
            int friendNumber = ProfileManager.Instance.Tox.AddFriendNoRequest(new ToxKey(ToxKeyType.Public, CurrentFriendRequest.PublicKey), out error);

            if (error != ToxErrorFriendAdd.Ok)
            {
                Debugging.Write("Failed to add friend: " + error);
            }
            else
            {
                var model = new FriendControlViewModel(this);
                model.ChatNumber = friendNumber;
                model.Name = ProfileManager.Instance.Tox.GetFriendPublicKey(friendNumber).ToString();

                //add the friend to the list, sorted
                AddObject(model);
                SortObject(model);

                //auto switch to converation view of this friend (?)
                SelectObject(model);
            }

            RemoveCurrentFriendRequest();
        }

        public void RemoveCurrentFriendRequest()
        {
            if (CurrentFriendRequest == null)
                return;

            _friendRequests.Remove(CurrentFriendRequest);

            OnPropertyChanged(() => CurrentFriendRequest);
            OnPropertyChanged(() => PendingFriendRequestsAvailable);
            OnPropertyChanged(() => PendingFriendRequestCount);
        }

        private void Tox_OnGroupInvite(object sender, ToxEventArgs.GroupInviteEventArgs e)
        {
            int groupNumber = ProfileManager.Instance.Tox.JoinGroup(e.FriendNumber, e.Data);

            MainWindow.Instance.UInvoke(() =>
            {
                var model = new GroupControlViewModel();
                model.ChatNumber = groupNumber;
                model.Name = ProfileManager.Instance.Tox.GetGroupTitle(groupNumber);

                //add the friend to the list, sorted
                AddObject(model);
                SortObject(model);

                //auto switch to converation view of this friend (?)
                SelectObject(model);
            });
        }

        private void Tox_OnFriendTypingChanged(object sender, ToxEventArgs.TypingStatusEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.IsTyping = e.IsTyping;
            });
        }

        private void Tox_OnFriendRequestReceived(object sender, ToxEventArgs.FriendRequestEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var request = new FriendRequest(e.PublicKey.ToString(), e.Message);
                _friendRequests.Add(request);

                OnPropertyChanged(() => CurrentFriendRequest);
                OnPropertyChanged(() => PendingFriendRequestsAvailable);
                OnPropertyChanged(() => PendingFriendRequestCount);
            });
        }
        
        private void Tox_OnReadReceiptReceived(object sender, ToxEventArgs.ReadReceiptEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                var msg = friend.ConversationView.Messages.FirstOrDefault(m => m is MessageViewModel && (m as MessageViewModel).MessageId == e.Receipt) as MessageViewModel;
                if (msg == null)
                {
                    Debugging.Write("Received a read receipt for a message we don't know about!");
                    return;
                }

                msg.WasReceived = true;
            });
        }

        private void Tox_OnFriendMessageReceived(object sender, ToxEventArgs.FriendMessageEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                var msg = new MessageViewModel(e.FriendNumber);
                msg.FriendName = ProfileManager.Instance.Tox.GetFriendName(e.FriendNumber);
                msg.Message = e.Message;
                msg.Time = DateTime.Now.ToShortTimeString();

                (friend.ConversationView as ConversationViewModel).AddMessage(msg);

                //if this is the first unread message, set HasUnreadMessages to true to make sure the status indicator gets updated
                if (!friend.HasUnreadMessages && !friend.IsSelected) friend.HasUnreadMessages = true;
            });
        }

        private void Tox_OnFriendConnectionStatusChanged(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.ConnectionStatus = e.Status;
                friend.IsOnline = e.Status != ToxConnectionStatus.None;

                SortObject(friend);
            });
        }

        private void Tox_OnFriendStatusChanged(object sender, ToxEventArgs.StatusEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.UserStatus = e.Status;
                SortObject(friend);
            });
        }

        private void Tox_OnFriendStatusMessageChanged(object sender, ToxEventArgs.StatusMessageEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.StatusMessage = e.StatusMessage;
            });
        }

        private void Tox_OnFriendNameChanged(object sender, ToxEventArgs.NameChangeEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.Name = e.Name;
                SortObject(friend);
            });
        }

        public IFriendObject FindFriend(int friendNumber)
        {
            return (IFriendObject)ChatCollection.FirstOrDefault(f => f is IFriendObject && f.ChatNumber == friendNumber);
        }

        public IGroupObject FindGroup(int groupNumber)
        {
            return (IGroupObject)ChatCollection.FirstOrDefault(f => f is IGroupObject && f.ChatNumber == groupNumber);
        }

        private static int GetStatusPriority(bool isOnline, ToxUserStatus status)
        {
            if (!isOnline)
                return 4;

            switch (status)
            {
                case ToxUserStatus.None:
                    return 0;
                case ToxUserStatus.Away:
                    return 1;
                case ToxUserStatus.Busy:
                    return 2;
                default:
                    return 3;
            }
        }
    }
}
