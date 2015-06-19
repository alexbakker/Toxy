using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

using Toxy.ViewModels;
using Toxy.Extensions;
using SharpTox.Core;
using System.ComponentModel;
using Toxy.Managers;

namespace Toxy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow _instance;
        public static MainWindow Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MainWindow();

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public MainWindowViewModel ViewModel
        {
            get { return DataContext as MainWindowViewModel; }
        }

        public void Reload()
        {
            if (_instance.Visibility == Visibility.Visible)
            {
                //this feels like a hack
                var handler = (CancelEventHandler)((sender, e) =>
                {
                    _instance = new MainWindow();
                    _instance.Show();
                });

                _instance.Closing += handler;
                _instance.Close();
            }
            else
            {
                _instance.Show();
            }
        }

        private MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();

            ProfileManager.Instance.Tox.OnFriendRequestReceived += Tox_OnFriendRequestReceived;
        }

        private void Tox_OnFriendRequestReceived(object sender, ToxEventArgs.FriendRequestEventArgs e)
        {
            MainWindow.Instance.UInvoke(() =>
            {
                var result = MessageBox.Show(string.Format("{0} wants to add you to his/her friend list.\n\nMessage: {1}\n\nWould you like to accept this friend request?", e.PublicKey.ToString(), e.Message), "New friend request", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                var error = ToxErrorFriendAdd.Ok;
                int friendNumber = ProfileManager.Instance.Tox.AddFriendNoRequest(e.PublicKey, out error);

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
                    MainWindow.Instance.ViewModel.CurrentFriendListView.AddObject(model);
                    MainWindow.Instance.ViewModel.CurrentFriendListView.SortObject(model);

                    //auto switch to converation view of this friend (?)
                    MainWindow.Instance.ViewModel.CurrentFriendListView.SelectObject(model);
                }
            });
        }

        private void ButtonGroup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed)
                return;

            DeselectFriendList();
        }

        private void ButtonTransfers_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed)
                return;

            DeselectFriendList();
        }

        private void ButtonAddFriend_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed)
                return;

            ViewModel.CurrentView = new AddFriendViewModel();
            DeselectFriendList();
        }

        private void ButtonSettings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed)
                return;

            ViewModel.CurrentView = ViewModel.CurrentSettingsView;
            DeselectFriendList();
        }

        //TODO: make this work in xaml somehow?
        private void DeselectFriendList()
        {
            ViewModel.CurrentFriendListView.SelectedChat = null;
        }
    }
}
