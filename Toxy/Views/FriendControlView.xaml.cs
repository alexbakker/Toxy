﻿using System;
using System.Windows;
using System.Windows.Controls;
using Toxy.ViewModels;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for FriendControlVIew.xaml
    /// </summary>
    public partial class FriendControlView : UserControl
    {
        public FriendControlViewModel Context { get { return DataContext as FriendControlViewModel; } }

        public FriendControlView()
        {
            InitializeComponent();
        }

        private void CopyPubKey_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Clipboard.SetText(App.Tox.GetFriendPublicKey(Context.ChatNumber).ToString());
        }

        private void RemoveFriend_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var result = MessageBox.Show(string.Format("Are you sure you want to delete {0} from your friend list?", Context.Name), "Delete friend", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            if (!(MainWindow.Instance.ViewModel.CurrentFriendListView.RemoveObject(Context) && App.Tox.DeleteFriend(Context.ChatNumber)))
            {
                Debugging.Write("Could not remove friend");
            }
        }

        private void ClearScrollback_Click(object sender, RoutedEventArgs e)
        {
            Context.ConversationView.Messages.Clear();
        }
    }
}
