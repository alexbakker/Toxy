using System;
using System.Linq;
using System.Collections.ObjectModel;
using SharpTox.Core;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class FriendListViewModel : ViewModelBase
    {
        public FriendListViewModel(MainWindowViewModel model)
        {
            MainWindow = model;
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

        //we have to keep a reference of the main view model in order to change the current view from here
        public readonly MainWindowViewModel MainWindow;

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
                    MainWindow.CurrentView = _selectedChat.ConversationView;
                }

                OnPropertyChanged(() => SelectedChat);
            }
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

        public void AddObject(IChatObject obj)
        {
            ChatCollection.Add(obj);
        }

        public bool RemoveObject(IChatObject obj)
        {
            var item = ChatCollection.FirstOrDefault();
            if (item != null)
                MainWindow.CurrentView = item.ConversationView;
            else
                MainWindow.CurrentView = new AddFriendViewModel();

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
    }
}
