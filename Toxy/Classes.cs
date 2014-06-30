using System.IO;
using System.Threading;

namespace Toxy
{
    public class MessageData
    {
        public string Username { get; set; }
        public string Message { get; set; }
    }

    public class FileTransfer
    {
        public Thread Thread { get; set; }
        public int FriendNumber { get; set; }
        public int FileNumber { get; set; }
        public ulong FileSize { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }
        public bool Finished { get; set; }
        public bool IsSender { get; set; }

        public FileTransferControl Control { get; set; }
    }
}
