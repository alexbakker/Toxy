using SharpTox.Core;
using System;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class MessageViewModel : ViewModelBase, IMessage
    {
        private string _message;
        private string _friendName; //TODO: make this a binding to the friend view model (performace impact?)
        //private string _time; //making this a datetime object is probably a very bad idea
        //private ToxMessageType _type;

        public int FriendNumber { get; set; }
        public int MessageId { get; set; }

        public MessageViewModel(int friendNumber)
        {
            //-1 is 'self' here
            FriendNumber = friendNumber;
            WasReceived = friendNumber != -1;
        }

        public string FriendName
        {
            get { return _friendName; }
            set { _friendName = value; }
            /*set
            {
                if (Equals(value, _friendName))
                {
                    return;
                }
                _friendName = value;
                OnPropertyChanged(() => FriendName);
            }*/
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
            /*set
            {
                if (Equals(value, _message))
                {
                    return;
                }
                _message = value;
                OnPropertyChanged(() => Message);
            }*/
        }

        private string _time;
        public string Time
        {
            get { return _time; }
            set { _time = value; }
            /*set
            {
                if (Equals(value, _time))
                {
                    return;
                }
                _time = value;
                OnPropertyChanged(() => Time);
            }*/
        }

        private bool _wasReceived;
        public bool WasReceived
        {
            get { return _wasReceived; }
            set
            {
                if (Equals(value, _wasReceived))
                {
                    return;
                }
                _wasReceived = value;
                OnPropertyChanged(() => WasReceived);
            }
        }

        public MessageType MessageType
        {
            get { return MessageType.Message; }
        }
    }
}
