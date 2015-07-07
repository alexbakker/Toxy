using SharpTox.Core;
using System;

namespace Toxy.Managers
{
    public class FileTransfer : IEquatable<FileTransfer>
    {
        public readonly int FileNumber;
        public readonly int FriendNumber;
        public readonly ToxFileKind Kind;
        public readonly FileTransferDirection Direction;

        public string Name { get; set; }
        public long Size { get; set; }
        public long TransferredBytes { get; set; }
        public string Path { get; set; }
        public bool IsPaused { get; private set; }

        public delegate void TransferBoolEvent(bool value);

        public event TransferBoolEvent OnStopped;
        public event EventHandler OnStarted;
        public event TransferBoolEvent OnPaused;
        public event EventHandler OnResumed;

        public FileTransfer(int fileNumber, int friendNumber, ToxFileKind kind, FileTransferDirection direction)
        {
            FileNumber = fileNumber;
            FriendNumber = friendNumber;
            Kind = kind;
            Direction = direction;
        }

        public bool Equals(FileTransfer other)
        {
            return (FileNumber == other.FileNumber && FriendNumber == other.FriendNumber);
        }

        public override int GetHashCode()
        {
            return FriendNumber | (FileNumber << 1);
        }

        public void Stop(bool force)
        {
            if (OnStopped != null)
                OnStopped(force);
        }

        public void Start()
        {
            if (OnStarted != null)
                OnStarted(this, new EventArgs());
        }

        public void Pause(bool isSelf)
        {
            IsPaused = true;

            if (OnPaused != null)
                OnPaused(isSelf);
        }

        public void Resume()
        {
            IsPaused = false;

            if (OnResumed != null)
                OnResumed(this, new EventArgs());
        }
    }

    public enum FileTransferDirection
    {
        Incoming,
        Outgoing
    }
}
