using SharpTox.Core;
using System;
using System.Diagnostics;
using System.IO;

namespace Toxy.Common.Transfers
{
    public class FileSender : FileTransfer
    {
        private Stream _stream;

        public long SentBytes
        {
            get
            {
                return _stream.Position;
            }
        }

        public FileSender(Tox tox, int fileNumber, int friendNumber, long fileSize, string fileName, string path)
            : base(tox, fileNumber, friendNumber, fileSize, fileName, path) 
        {

        }

        public FileSender(Tox tox, int fileNumber, int friendNumber, long fileSize, string fileName, Stream stream)
            : base(tox, fileNumber, friendNumber, fileSize, fileName, string.Empty)
        {
            _stream = stream;
        }

        public void Start()
        {
            FileTransferWorker.StartTransfer(this);
            Tag.SetStatus(FileName);
        }

        public override void Kill(bool finished)
        {
            FileTransferWorker.KillTransfer(this);

            if (_stream != null)
                _stream.Dispose();

            if (finished)
            {
                Tox.FileSendControl(FriendNumber, 0, FileNumber, ToxFileControl.Finished, new byte[0]);
                Finished = true;

                Tag.HideAllButtons();
            }
            else
            {
                Tox.FileSendControl(FriendNumber, 0, FileNumber, ToxFileControl.Kill, new byte[0]);
                Tag.HideAllButtons();
                Tag.SetStatus(FileName + " - Transfer killed");
            }
        }

        public void RewindStream(long index)
        {
            if (!Broken)
                return;

            _stream.Position = index;

            double value = (double)index / (double)FileSize;
            Progress = 100 - (int)(value * 100);
        }

        public bool SendNextChunk()
        {
            if (_stream == null)
                _stream = new FileStream(Path, FileMode.Open);

            int chunk_size = Tox.FileDataSize(FriendNumber);
            byte[] buffer = new byte[chunk_size];
            ulong remaining = Tox.FileDataRemaining(FriendNumber, FileNumber, 0);

            if (remaining > (ulong)chunk_size)
            {
                if (_stream.Read(buffer, 0, chunk_size) == 0)
                {
                    Kill(true);
                    return false;
                }

                if (!Tox.FileSendData(FriendNumber, FileNumber, buffer))
                {
                    _stream.Position -= chunk_size;
                    Debug.WriteLine("Could not send data, rewinding the stream");

                    return false;
                }

                Debug.WriteLine(string.Format("Data sent: {0} bytes", buffer.Length));
            }
            else
            {
                buffer = new byte[remaining];

                if (_stream.Read(buffer, 0, (int)remaining) == 0)
                {
                    Kill(true);
                    return false;
                }

                Tox.FileSendData(FriendNumber, FileNumber, buffer);
                Debug.WriteLine(string.Format("Sent the last chunk of data: {0} bytes", buffer.Length));

                Kill(true);
            }

            double value = (double)remaining / (double)FileSize;
            Progress = 100 - (int)(value * 100);

            return true;
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
                _broken = value;

                if (value)
                {
                    FileTransferWorker.PauseTransfer(this);
                    Tag.SetStatus("Waiting for friend to come back online");
                }
                else
                {
                    FileTransferWorker.ResumeTransfer(this);
                    Tag.SetStatus(FileName);
                }
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

                if (value)
                {
                    FileTransferWorker.PauseTransfer(this);
                    Tag.SetStatus(FileName + " - Paused");
                }
                else
                {
                    FileTransferWorker.ResumeTransfer(this);
                    Tag.SetStatus(FileName);
                }
            }
        }
    }
}
