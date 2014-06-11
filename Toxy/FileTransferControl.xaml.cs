using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Toxy
{
    /// <summary>
    /// Interaction logic for FileTransferControl.xaml
    /// </summary>
    public partial class FileTransferControl : UserControl
    {
        private int filenumber;
        private int friendnumber;
        private string filename;
        private ulong filesize;

        public delegate void OnAcceptDelegate(int friendnumber, int filenumber);
        public event OnAcceptDelegate OnAccept;

        public delegate void OnDeclineDelegate(int friendnumber, int filenumber);
        public event OnDeclineDelegate OnDecline;

        public FileTransferControl(string friendname, int friendnumber, int filenumber, string filename, ulong filesize)
        {
            this.filenumber = filenumber;
            this.friendnumber = friendnumber;
            this.filesize = filesize;
            this.filename = filename;

            InitializeComponent();

            SizeLabel.Content = filesize.ToString() + " bytes";
            MessageLabel.Content = string.Format("{0} would like to share {1} with you", friendname, filename);
        }

        public void SetStatus(string status)
        {
            //I guess this doesn't do anything, for now
        }

        public void TransferFinished()
        {
            AcceptButton.Visibility = Visibility.Collapsed;
            DeclineButton.Visibility = Visibility.Collapsed;
        }

        public void TransferStarted()
        {
            AcceptButton.Visibility = Visibility.Collapsed;
            TransferProgressRing.Visibility = Visibility.Visible;
        }

        public void SetProgress(int value)
        {
            TransferProgressBar.Value = value;
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnAccept != null)
                OnAccept(friendnumber, filenumber);

            MessageLabel.Content = filename;
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnDecline != null)
                OnDecline(friendnumber, filenumber);

            MessageLabel.Content = "Canceled";

            TransferFinished();
        }
    }
}
