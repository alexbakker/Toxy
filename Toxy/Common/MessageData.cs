using System;

namespace Toxy.Common
{
    public class MessageData
    {
        private readonly string _username;
        public string Username
        {
            get { return _username; }
        }
        public string Message { get; set; }

        private readonly int _id;
        public int Id
        {
            get { return _id; }
        }

        private readonly bool _isAction;
        public bool IsAction
        {
            get { return _isAction; }
        }

        private readonly bool _isSelf;
        public bool IsSelf
        {
            get { return _isSelf; }
        }

        private readonly bool _isGroupMsg;
        public bool IsGroupMsg
        {
            get { return _isGroupMsg; }
        }

        private readonly DateTime _timestamp;
        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        public MessageData(int id, string username, string message, bool isAction, DateTime timestamp, bool isGroupMsg,
            bool isSelf)
        {
            _id = id;
            _username = username;
            Message = message;
            _isAction = isAction;
            _timestamp = timestamp;
            _isGroupMsg = isGroupMsg;
            _isSelf = isSelf;
        }
    }
}
