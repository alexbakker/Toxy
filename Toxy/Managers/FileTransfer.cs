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

        public event EventHandler OnStopped;
        public event EventHandler OnStarted;

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

        public void Stop()
        {
            if (OnStopped != null)
                OnStopped(this, new EventArgs());
        }

        public void Start()
        {
            if (OnStarted != null)
                OnStarted(this, new EventArgs());
        }
    }

    public enum FileTransferDirection
    {
        Incoming,
        Outgoing
    }
}
