using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Toxy.ViewModels;
using Toxy.Extensions;
using SharpTox.Core;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for FriendListView.xaml
    /// </summary>
    public partial class FriendListView : UserControl
    {
        public FriendListViewModel Context { get { return DataContext as FriendListViewModel; } }

        public FriendListView()
        {
            Loaded += FriendListView_Loaded;
            InitializeComponent();

            App.Tox.OnFriendNameChanged += Tox_OnFriendNameChanged;
            App.Tox.OnFriendStatusMessageChanged += Tox_OnFriendStatusMessageChanged;
            App.Tox.OnFriendStatusChanged += Tox_OnFriendStatusChanged;
            App.Tox.OnFriendConnectionStatusChanged += Tox_OnFriendConnectionStatusChanged;
            App.Tox.OnFriendMessageReceived += Tox_OnFriendMessageReceived;
            App.Tox.OnReadReceiptReceived += Tox_OnReadReceiptReceived;
        }

        private void Tox_OnReadReceiptReceived(object sender, ToxEventArgs.ReadReceiptEventArgs e)
        {
            this.UInvoke(() =>
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
            this.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                var msg = new MessageViewModel(e.FriendNumber);
                msg.FriendName = App.Tox.GetFriendName(e.FriendNumber);
                msg.Message = e.Message;
                msg.Time = DateTime.Now.ToShortTimeString();

                friend.ConversationView.AddMessage(msg);

                //if this is the first unread message, set HasUnreadMessages to true to make sure the status indicator gets updated
                if (!friend.HasUnreadMessages && !friend.IsSelected) friend.HasUnreadMessages = true;
            });
        }

        private void Tox_OnFriendConnectionStatusChanged(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            this.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.ConnectionStatus = e.Status;
                friend.IsOnline = e.Status != ToxConnectionStatus.None;

                Context.SortObject(friend);
            });
        }

        private void Tox_OnFriendStatusChanged(object sender, ToxEventArgs.StatusEventArgs e)
        {
            this.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.UserStatus = e.Status;
                Context.SortObject(friend);
            });
        }

        private void Tox_OnFriendStatusMessageChanged(object sender, ToxEventArgs.StatusMessageEventArgs e)
        {
            this.UInvoke(() =>
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
            this.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("We don't know about this friend!");
                    return;
                }

                friend.Name = e.Name;
                Context.SortObject(friend);
            });
        }

        private void FriendListView_Loaded(object sender, RoutedEventArgs e)
        {
            //this event can be fired more than once but we only want to do the initial friend list population once.
            if (Context.ChatCollection.Count != 0)
                return;

            foreach (int friend in App.Tox.Friends)
            {
                string name = App.Tox.GetFriendName(friend);
                string statusMessage = App.Tox.GetFriendStatusMessage(friend);

                var model = new FriendControlViewModel();
                model.ChatNumber = friend;
                model.Name = string.IsNullOrEmpty(name) ? App.Tox.GetFriendPublicKey(friend).ToString() : name;
                model.StatusMessage = statusMessage;

                Context.AddObject(model);
            }

            Context.RearrangeChatCollection();
        }

        private IChatObject FindFriend(int friendNumber)
        {
            return Context.ChatCollection.FirstOrDefault(f => f.ChatNumber == friendNumber);
        }
    }
}
