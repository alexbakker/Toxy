using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SharpTox.Core;
using Toxy.ViewModels;
using Toxy.Managers;
using Toxy.Tools;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for AddFriendView.xaml
    /// </summary>
    public partial class AddFriendView : UserControl
    {
        public AddFriendView()
        {
            InitializeComponent();
        }

        private async void ButtonAddFriend_Click(object sender, RoutedEventArgs e)
        {
            string id = TextBoxFriendId.Text.Trim();
            string message = TextBoxMessage.Text.Trim();

            if (string.IsNullOrEmpty(id))
            {
                ShowError("The Tox ID field is empty.");
                return;
            }

            if (string.IsNullOrEmpty(message))
                message = (string)TextBoxMessage.Tag;

            if (Config.Instance.EnableToxMe && id.Contains("@"))
            {
                string[] parts = id.Split('@');
                var api = new ToxMeApi(parts[1]);

                try { id = api.LookupID(parts[0]); }
                catch (Exception ex)
                {
                    ShowError("Lookup failed, " + ex.Message);
                    return;
                }

                if (id == null || !ToxId.IsValid(id))
                {
                    ShowError("The retrieved Tox ID is invalid.");
                    return;
                }

                TextBoxFriendId.Text = id;
                return;
            }

            if (!ToxId.IsValid(id))
            {
                ShowError("The entered Tox ID is invalid.");
                return;
            }

            var error = ToxErrorFriendAdd.Ok;
            int friendNumber = ProfileManager.Instance.Tox.AddFriend(new ToxId(id), message, out error);

            if (error != ToxErrorFriendAdd.Ok)
            {
                ShowError(error.ToString());
                return;
            }

            var model = new FriendControlViewModel(MainWindow.Instance.ViewModel.CurrentFriendListView);
            model.ChatNumber = friendNumber;
            model.Name = id;
            model.StatusMessage = "Friend request sent";

            MainWindow.Instance.ViewModel.CurrentFriendListView.AddObject(model);
            MainWindow.Instance.ViewModel.CurrentFriendListView.SortObject(model);
            MainWindow.Instance.ViewModel.CurrentFriendListView.SelectObject(model);

            await ProfileManager.Instance.SaveAsync();
        }

        private void ShowError(string message)
        {
            MessageBox.Show("Could not send friend request: " + message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
