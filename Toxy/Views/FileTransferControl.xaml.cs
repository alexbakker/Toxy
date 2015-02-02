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
using Toxy.ToxHelpers;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for FileTransferControl.xaml
    /// </summary>
    public partial class FileTransferControl : UserControl
    {
        private FileTransfer transfer;
        private TableCell fileTableCell;

        public delegate void FileTransferEventDelegate(FileTransfer transfer);
        public event FileTransferEventDelegate OnAccept;
        public event FileTransferEventDelegate OnPause;
        public event FileTransferEventDelegate OnDecline;
        public event FileTransferEventDelegate OnFileOpen;
        public event FileTransferEventDelegate OnFolderOpen;

        public string FilePath { get; set; }

        public FileTransferControl(FileTransfer transfer, TableCell fileTableCell)
        {
            this.transfer = transfer;
            this.fileTableCell = fileTableCell;

            InitializeComponent();

            SizeLabel.Content = Tools.GetSizeString(transfer.FileSize);
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
            ResumeButton.Visibility = Visibility.Collapsed;
            PauseButton.Visibility = Visibility.Collapsed;

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

            TransferFinished();
        }

        private void FileOpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (OnFileOpen != null)
                OnFileOpen(transfer);
        }

        private void FolderOpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (OnFolderOpen != null)
                OnFolderOpen(transfer);
        }

        public void HideAllButtons()
        {
            Dispatcher.BeginInvoke(((Action)(() => 
            {
                AcceptButton.Visibility = Visibility.Collapsed;
                DeclineButton.Visibility = Visibility.Collapsed;
                FileOpenButton.Visibility = Visibility.Collapsed;
                FolderOpenButton.Visibility = Visibility.Collapsed;
                ResumeButton.Visibility = Visibility.Collapsed;
                PauseButton.Visibility = Visibility.Collapsed;
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

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            transfer.Paused = !transfer.Paused;
            if (transfer.Paused)
            {
                ResumeButton.Visibility = Visibility.Visible;
                PauseButton.Visibility = Visibility.Hidden;
            }
            else
            {
                ResumeButton.Visibility = Visibility.Hidden;
                PauseButton.Visibility = Visibility.Visible;
            }

            if (OnPause != null)
                OnPause(transfer);
        }

        public void StartTransfer()
        {
            Dispatcher.BeginInvoke(((Action)(() => PauseButton.Visibility = Visibility.Visible)));
        }
    }
}
