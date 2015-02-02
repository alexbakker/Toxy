using SharpTox.Core;
using Toxy.Views;

namespace Toxy.Common.Transfers
{
    public abstract class FileTransfer
    {
        public int FileNumber { get; private set; }
        public int FriendNumber { get; private set; }
        public long FileSize { get; private set; }

        public string Path { get; set; }
        public string FileName { get; private set; }

        private int _progress;
        public int Progress
        {
            get
            {
                return _progress;
            }
            protected set
            {
                if (Tag != null)
                    Tag.SetProgress(value);

                _progress = value;
            }
        }

        public bool Finished { get; protected set; }
        public abstract bool Broken { get; set; }
        public abstract bool Paused { get; set; }

        public FileTransferControl Tag { get; set; }
        public Tox Tox { get; private set; }

        public abstract void Kill(bool finished);

        public FileTransfer(Tox tox, int fileNumber, int friendNumber, long fileSize, string fileName, string path)
        {
            Tox = tox;
            FileNumber = fileNumber;
            FriendNumber = friendNumber;
            FileSize = fileSize;
            FileName = fileName;
            Path = path;
        }
    }
}
