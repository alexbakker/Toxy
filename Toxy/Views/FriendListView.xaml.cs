using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Toxy.ViewModels;
using Toxy.Extensions;
using SharpTox.Core;
using Toxy.Managers;

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
        }

        private void FriendListView_Loaded(object sender, RoutedEventArgs e)
        {
            //this event can be fired more than once but we only want to do the initial friend list population once.
            if (Context.ChatCollection.Count != 0)
                return;

            foreach (int friend in ProfileManager.Instance.Tox.Friends)
            {
                string name = ProfileManager.Instance.Tox.GetFriendName(friend);
                string statusMessage = ProfileManager.Instance.Tox.GetFriendStatusMessage(friend);

                var model = new FriendControlViewModel();
                model.ChatNumber = friend;
                model.Name = string.IsNullOrEmpty(name) ? ProfileManager.Instance.Tox.GetFriendPublicKey(friend).ToString() : name;
                model.StatusMessage = statusMessage;

                Context.AddObject(model);
            }

            Context.RearrangeChatCollection();
        }
    }
}
