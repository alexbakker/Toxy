using Microsoft.Win32;
using SharpTox.Core;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Toxy.Managers;
using Toxy.ViewModels;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for FileTransferView.xaml
    /// </summary>
    public partial class FileTransferView : UserControl
    {
        public FileTransferViewModel Context { get { return DataContext as FileTransferViewModel; } }

        public FileTransferView()
        {
            InitializeComponent();
        }

        private void TopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Context.IsFinished && !Context.IsInProgress && Context.Direction == FileTransferDirection.Incoming)
            {
                var dialog = new SaveFileDialog();
                dialog.FileName = Context.Transfer.Name;
                dialog.OverwritePrompt = true;
                dialog.ValidateNames = true;

                if (dialog.ShowDialog() != true)
                    return;

                TransferManager.Instance.AcceptTransfer(Context.Transfer, dialog.FileName); //cancel the transfer if this fails
            }
            else if (Context.IsInProgress || (!Context.IsFinished && Context.Direction == FileTransferDirection.Outgoing))
            {
                TransferManager.Instance.CancelTransfer(Context.Transfer);
            }
        }

        private void BottomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Context.IsFinished && !Context.IsInProgress && Context.Direction == FileTransferDirection.Incoming)
            {
                TransferManager.Instance.CancelTransfer(Context.Transfer);
            }
            else if (Context.IsInProgress && !Context.IsPaused)
            {
                TransferManager.Instance.PauseTransfer(Context.Transfer);
            }
            else if (Context.IsInProgress && Context.IsSelfPaused)
            {
                TransferManager.Instance.ResumeTransfer(Context.Transfer);
            }
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (Context.Transfer.Path == null)
                return;

            try { Process.Start("explorer.exe", @"/select, " + Context.Transfer.Path); }
            catch (Exception ex) { Debugging.Write("Could not open folder:, exception: " + ex.ToString()); }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var path = Context.Transfer.Path;
            if (path == null)
                return;

            if (path != null && path.EndsWith(".exe"))
            {
                var result = MessageBox.Show("Opening executable files could be harmful to your device, do you wish to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            try { Process.Start(path); }
            catch (Exception ex) { Debugging.Write("Could not open file:, exception: " + ex.ToString()); }
        }
    }
}
