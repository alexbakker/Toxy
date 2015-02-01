using System;
using System.IO;

using SharpTox.Core;
using System.Windows;

namespace Toxy.Common.Transfers
{
    public class FileReceiver : FileTransfer
    {
        private FileStream _stream;

        public long BytesReceived { get; set; }

        public FileReceiver(Tox tox, int fileNumber, int friendNumber, long fileSize, string fileName, string path)
            : base(tox, fileNumber, friendNumber, fileSize, fileName, path) { }

        public void ProcessReceivedData(byte[] data)
        {
            if (_stream == null)
                _stream = new FileStream(Path, FileMode.Create);

            _stream.Write(data, 0, data.Length);

            BytesReceived += data.Length;
            Progress = (int)(((double)BytesReceived / (double)FileSize) * 100d);
        }

        public override void Kill(bool finished)
        {
            if (_stream != null)
                _stream.Dispose();

            if (finished)
            {
                Tox.FileSendControl(FriendNumber, 1, FileNumber, ToxFileControl.Finished, new byte[0]);
                Finished = true;

                Tag.AcceptButton.Visibility = Visibility.Collapsed;
                Tag.DeclineButton.Visibility = Visibility.Collapsed;
                Tag.FileOpenButton.Visibility = Visibility.Visible;
                Tag.FolderOpenButton.Visibility = Visibility.Visible;
            }
            else
            {
                Tox.FileSendControl(FriendNumber, 1, FileNumber, ToxFileControl.Kill, new byte[0]);
            }
        }

        private bool _broken;
        public override bool Broken
        {
            get
            {
                return _broken;
            }
            set
            {
                if (value)
                    Tag.SetStatus("Waiting for friend to come back online");
                else
                    Tag.SetStatus(FileName);

                _broken = value;
            }
        }

        private bool _paused;
        public override bool Paused
        {
            get
            {
                return _paused;
            }
            set
            {
                _paused = value;
            }
        }
    }
}
