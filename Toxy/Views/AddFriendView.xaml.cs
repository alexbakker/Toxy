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

            if (Config.Instance.EnableToxDns && id.Contains("@"))
            {
                //try resolving 3 times
                for (int tries = 0; tries < 3; tries++)
                {
                    try
                    {
                        string toxId = DnsUtils.DiscoverToxID(id, Config.Instance.NameServices, !Config.Instance.AllowPublicKeyLookups, !Config.Instance.AllowTox1Lookups);
                        if (!string.IsNullOrEmpty(toxId))
                        {
                            //show the tox id to the user before actually adding it to the friend list
                            TextBoxFriendId.Text = toxId;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debugging.Write(string.Format("Could not resolve {0}: {1}", id, ex.Message));
                    }
                }

                //if we got this far the discovery must have failed
                ShowError("Could not resolve tox username.");
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
