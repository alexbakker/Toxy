using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

using Toxy.Common;
using Toxy.Common.Transfers;
using Toxy.Extenstions;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for FileTransferControl.xaml
    /// </summary>
    public partial class FileTransferControl : UserControl
    {
        private FileTransfer transfer;
        private TableCell fileTableCell;

        public delegate void OnAcceptDelegate(FileTransfer transfer);
        public event OnAcceptDelegate OnAccept;

        public delegate void OnDeclineDelegate(FileTransfer transfer);
        public event OnDeclineDelegate OnDecline;

        public delegate void OnFileOpenDelegate();
        public event OnFileOpenDelegate OnFileOpen;

        public delegate void OnFolderOpenDelegate();
        public event OnFolderOpenDelegate OnFolderOpen;

        public string FilePath { get; set; }

        public FileTransferControl(FileTransfer transfer, TableCell fileTableCell)
        {
            this.transfer = transfer;
            this.fileTableCell = fileTableCell;

            InitializeComponent();

            SizeLabel.Content = transfer.FileSize.ToString() + " bytes";
            MessageLabel.Content = string.Format(transfer.FileName);
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
                OnAccept(transfer);

            AcceptButton.Visibility = Visibility.Collapsed;
            MessageLabel.Content = transfer.FileName;
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnDecline != null)
                OnDecline(transfer);

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
