using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SharpTox.Core;
using Toxy.ViewModels;
using Toxy.Extensions;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Toxy.Managers;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for SelfView.xaml
    /// </summary>
    public partial class SelfView : UserControl
    {
        public SelfViewModel Context { get { return DataContext as SelfViewModel; } }

        public SelfView()
        {
            InitializeComponent();

            App.Tox.OnConnectionStatusChanged += Tox_OnConnectionStatusChanged;
        }

        private void Tox_OnConnectionStatusChanged(object sender, ToxEventArgs.ConnectionStatusEventArgs e)
        {
            this.UInvoke(() => Context.ConnectionStatus = e.Status);
        }

        private void ButtonUserStatus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ButtonUserStatus.ContextMenu.PlacementTarget = ButtonUserStatus;
                ButtonUserStatus.ContextMenu.IsOpen = true;
            }
        }

        private void SelfView_Loaded(object sender, RoutedEventArgs e)
        {
            Context.Name = App.Tox.Name;
            Context.StatusMessage = App.Tox.StatusMessage;
            Context.UserStatus = App.Tox.Status;
        }

        private void ContextMenuItemStatus_Click(object sender, RoutedEventArgs e)
        {
            var status = (ToxUserStatus)((MenuItem)e.Source).Tag; //king of casting

            App.Tox.Status = status;
            Context.UserStatus = status;
        }

        private void RemoveAvatar_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ViewModel.CurrentSelfView.Avatar = null;
        }

        private void EditAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            dialog.Filter = "Image files (*.png, *.gif, *.jpeg, *.jpg) | *.png;*.gif;*.jpeg;*.jpg";

            if (dialog.ShowDialog() != true)
                return;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(dialog.FileName);
            bmp.DecodePixelWidth = 128; //this should make the file size smaller than 1 << 16
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            
            byte[] bytes = BitmapImageToBytes(bmp);
            AvatarManager.Instance.SaveAvatar(App.Tox.Id.PublicKey.ToString(), bytes);

            foreach(int friend in App.Tox.Friends)
                if (App.Tox.IsFriendOnline(friend))
                    TransferManager.SendAvatar(friend, bytes);

            MainWindow.Instance.ViewModel.CurrentSelfView.Avatar = bmp;
        }

        private static byte[] BitmapImageToBytes(BitmapImage bmp)
        {
            var ms = new MemoryStream();
            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(bmp));
            pngEncoder.Save(ms);

            return ms.GetBuffer();
        }
    }
}
