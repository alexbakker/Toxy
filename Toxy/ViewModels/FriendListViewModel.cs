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

            Init();
        }

        private void Init()
        {
            foreach (int friend in ProfileManager.Instance.Tox.Friends)
            {
                string name = ProfileManager.Instance.Tox.GetFriendName(friend);
                string statusMessage = ProfileManager.Instance.Tox.GetFriendStatusMessage(friend);

                var model = new FriendControlViewModel();
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

        private FriendControlViewModel _selectedChat;
        public FriendControlViewModel SelectedChat
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
                    MainWindowView.CurrentView = _selectedChat.ConversationView;
                }

                OnPropertyChanged(() => SelectedChat);
            }
        }

        public void AddObject(IChatObject obj)
        {
            ChatCollection.Add(obj);
        }

        public bool RemoveObject(IChatObject obj)
        {
            var item = ChatCollection.FirstOrDefault();
            if (item != null)
                MainWindowView.CurrentView = item.ConversationView;
            else
                MainWindowView.CurrentView = new AddFriendViewModel();

            return ChatCollection.Remove(obj);
        }

        public void SortObject(IChatObject obj)
        {
            //this procedure may seem silly but will be better performance-wise compared to calling RearrangeChatCollection all day
            var tempColl = new ObservableCollection<IChatObject>(
                ChatCollection.OrderBy(chat =>
                    GetStatusPriority(chat.IsOnline, chat.UserStatus)).
                    ThenBy(chat => chat.Name)
                );

            int oldIndex = ChatCollection.IndexOf(obj);
            int newIndex = tempColl.IndexOf(obj);

            if (oldIndex != newIndex)
                ChatCollection.Move(oldIndex, newIndex);
        }

        public void SelectObject(FriendControlViewModel obj)
        {
            SelectedChat = obj;
        }

        //this should only be used after initially populating the friend list
        public void RearrangeChatCollection()
        {
            ChatCollection = new ObservableCollection<IChatObject>(
                ChatCollection.OrderBy(chat =>
                    GetStatusPriority(chat.IsOnline, chat.UserStatus)).
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
                var model = new FriendControlViewModel();
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

                var msg = friend.ConversationView.Messages.FirstOrDefault(m => m.MessageType == MessageType.Message && m.MessageId == e.Receipt);
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

                friend.ConversationView.AddMessage(msg);

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

        private IChatObject FindFriend(int friendNumber)
        {
            return ChatCollection.FirstOrDefault(f => f.ChatNumber == friendNumber);
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
