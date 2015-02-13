using System;
using System.IO;
using System.Threading;
using Toxy.Views;

namespace Toxy.Common
{
    public class MessageData
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public int Id { get; set; }
        public bool IsAction { get; set; }
        public bool IsSelf { get; set; }
        public bool IsGroupMsg { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
