using System;
using System.Windows.Controls;
using System.Windows.Input;
using Toxy.ViewModels;

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
            InitializeComponent();
        }

        private void AcceptRequest_Click(object sender, MouseButtonEventArgs e)
        {
            Context.AcceptCurrentFriendRequest();
        }

        private void DeclineRequest_Click(object sender, MouseButtonEventArgs e)
        {
            Context.RemoveCurrentFriendRequest();
        }
    }
}
