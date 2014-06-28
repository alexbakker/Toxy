using System.IO;

namespace Toxy
{
    public class MessageData
    {
        public string Username { get; set; }
        public string Message { get; set; }
    }

    public class FileTransfer
    {
        public int FriendNumber { get; set; }
        public int FileNumber { get; set; }
        public ulong FileSize { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }

        public FileTransferControl Control { get; set; }
    }
}
