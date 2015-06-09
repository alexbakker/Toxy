using System;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class FileTransferViewModel : ViewModelBase, IMessage
    {
        public int FriendNumber { get; set; }

        public FileTransferViewModel(int friendNumber)
        {
            //-1 is 'self' here
            FriendNumber = friendNumber;
        }

        private string _name = "unknown";
        public string Name
        {
            get { return _name; }
            set
            {
                if (Equals(value, _name))
                {
                    return;
                }
                _name = value;
                OnPropertyChanged(() => Name);
            }
        }

        private string _size;
        public string Size
        {
            get { return _size; }
            set
            {
                if (Equals(value, _size))
                {
                    return;
                }
                _size = value;
                OnPropertyChanged(() => Size);
            }
        }

        private string _speed = "125KB/s";
        public string Speed
        {
            get { return _speed; }
            set
            {
                if (Equals(value, _speed))
                {
                    return;
                }
                _speed = value;
                OnPropertyChanged(() => Speed);
            }
        }

        private string _timeLeft = "13:37";
        public string TimeLeft
        {
            get { return _timeLeft; }
            set
            {
                if (Equals(value, _timeLeft))
                {
                    return;
                }
                _timeLeft = value;
                OnPropertyChanged(() => TimeLeft);
            }
        }

        private int _progress = 35;
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (Equals(value, _progress))
                {
                    return;
                }
                _progress = value;
                OnPropertyChanged(() => Progress);
            }
        }

        public MessageType MessageType
        {
            get { return MessageType.FileTransfer; }
        }

        public enum FileTransferState
        {
            AcceptReject,
            Rejected,
            Accepted,
            Sending,
            Receiving,
            Finished,
            Aborted
        }


        public int MessageId
        {
            get { return -1; }
            set { }
        }

        public bool WasReceived
        {
            get { return true; }
            set { }
        }
    }
}
