using SharpTox.Core;
using System;

namespace Toxy.Managers
{
    public class FileTransfer : IEquatable<FileTransfer>
    {
        public readonly int FileNumber;
        public readonly int FriendNumber;
        public readonly ToxFileKind Kind;

        public FileTransfer(int fileNumber, int friendNumber, ToxFileKind kind)
        {
            FileNumber = fileNumber;
            FriendNumber = friendNumber;
            Kind = kind;
        }

        public bool Equals(FileTransfer other)
        {
            return (FileNumber == other.FileNumber && FriendNumber == other.FriendNumber);
        }

        public override int GetHashCode()
        {
            return FriendNumber | (FileNumber << 1);
        }
    }
}
