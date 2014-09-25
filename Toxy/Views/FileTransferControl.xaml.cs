using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Toxy.Common;

namespace Toxy.Views
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
        private TableCell fileTableCell;

        public delegate void OnAcceptDelegate(int friendnumber, int filenumber);
        public event OnAcceptDelegate OnAccept;

        public delegate void OnDeclineDelegate(int friendnumber, int filenumber);
        public event OnDeclineDelegate OnDecline;

        public delegate void OnFileOpenDelegate();
        public event OnFileOpenDelegate OnFileOpen;

        public delegate void OnFolderOpenDelegate();
        public event OnFolderOpenDelegate OnFolderOpen;

        public string FilePath { get; set; }

        public FileTransferControl(int friendnumber, int filenumber, string filename, ulong filesize, TableCell fileTableCell)
        {
            this.filenumber = filenumber;
            this.friendnumber = friendnumber;
            this.filesize = filesize;
            this.filename = filename;
            this.fileTableCell = fileTableCell;

            InitializeComponent();

            SizeLabel.Content = filesize.ToString() + " bytes";
            MessageLabel.Content = string.Format(filename);
        }

        public void SetStatus(string status)
        {
            Dispatcher.BeginInvoke(((Action)(() => MessageLabel.Content = status)));
        }

        public void TransferFinished(bool complete = true)
        {
            AcceptButton.Visibility = Visibility.Collapsed;
            DeclineButton.Visibility = Visibility.Collapsed;
            FileOpenButton.Visibility = Visibility.Visible;
            FolderOpenButton.Visibility = Visibility.Visible;
            if (complete)
            {
                SetProgress(100);
                if (File.Exists(FilePath))
                {
                    var uri = new Uri(FilePath);
                    var absoluteUri = uri.AbsoluteUri;
                    AddThumbnail(fileTableCell, absoluteUri);
                }
            }
        }

        public void SetProgress(int value)
        {
            Dispatcher.BeginInvoke(((Action)(() => TransferProgressBar.Value = value)));
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnAccept != null)
                OnAccept(friendnumber, filenumber);

            AcceptButton.Visibility = Visibility.Collapsed;
            MessageLabel.Content = filename;
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnDecline != null)
                OnDecline(friendnumber, filenumber);

            MessageLabel.Content = "Canceled";

            TransferFinished();
        }

        private void FileOpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (OnFileOpen != null)
                OnFileOpen();
        }

        private void FolderOpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (OnFolderOpen != null)
                OnFolderOpen();
        }

        public void HideAllButtons()
        {
            Dispatcher.BeginInvoke(((Action)(() => {
                AcceptButton.Visibility = Visibility.Collapsed;
                DeclineButton.Visibility = Visibility.Collapsed;
                FileOpenButton.Visibility = Visibility.Collapsed;
                FolderOpenButton.Visibility = Visibility.Collapsed;
            })));
        }

        private void AddThumbnail(TableCell messageTableCell, string message)
        {
            var task = new Task(() =>
            {
                try
                {
                    if (message.IsImage())
                    {
                        var imagePath = message;
                        Dispatcher.Invoke(() =>
                        {
                            var thumbnail = new Paragraph();
                            var image = new Image();
                            var bitmapImage = new BitmapImage();

                            bitmapImage.BeginInit();
                            bitmapImage.UriSource = new Uri(imagePath);
                            bitmapImage.EndInit();
                            image.Source = bitmapImage;

                            thumbnail.Inlines.Add(image);
                            messageTableCell.Blocks.Add(thumbnail);
                        });
                    }
                }
                catch
                {

                }
            });
            task.Start();
        }
    }
}
