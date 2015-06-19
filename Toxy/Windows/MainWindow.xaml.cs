﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;

using SharpTox.Core;
using Toxy.ViewModels;
using Toxy.Extensions;
using Toxy.Managers;
using System.Threading.Tasks;
using Squirrel;

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

            //only check for updates once at launch (TODO: check periodically?)
            //TODO: move this someplace else
            CheckForUpdates();
        }

        private async Task CheckForUpdates()
        {
            try
            {
                using (var mgr = new UpdateManager("http://update.toxing.me/toxy"))
                {
                    //
                    if (!mgr.IsInstalledApp)
                    {
                        Debugging.Write("Skipping update check, this is not an installed application.");
                        return;
                    }

                    var updateInfo = await mgr.CheckForUpdate();

                    Debugging.Write("Currently installed: " + updateInfo.CurrentlyInstalledVersion.Version);
                    Debugging.Write("Latest version: " + updateInfo.FutureReleaseEntry.Version);

                    if (updateInfo.CurrentlyInstalledVersion.Version < updateInfo.FutureReleaseEntry.Version)
                    {
                        //download the latest release so we can retrieve the release notes
                        await mgr.DownloadReleases(new[] { updateInfo.FutureReleaseEntry });

                        string msg = string.Format("There is a new update available for installation. The latest version is {0}. Would you like to update?\n\nChanges:\n{1}", 
                            updateInfo.FutureReleaseEntry.Version,
                            updateInfo.FutureReleaseEntry.GetReleaseNotes(updateInfo.PackageDirectory));
                        

                        var result = MessageBox.Show(msg, "Updates available!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            await mgr.UpdateApp();

                            result = MessageBox.Show("Toxy has to be restarted in order to apply the update. Restart now?", "Updates successfully installed", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                //eww
                                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                                Application.Current.Shutdown();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debugging.Write("Could not check for updates: " + ex.ToString()); }
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