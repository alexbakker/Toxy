using SharpTox.Core;
using System;
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
                TransferManager.Instance.AcceptTransfer(Context.Transfer); //cancel the transfer if this fails
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
            else if (Context.IsInProgress)
            {
                TransferManager.Instance.PauseTransfer(Context.Transfer);
            }
        }
    }
}
