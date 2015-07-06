using System;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class FileTransferMessageViewModel : ViewModelBase
    {
        private FileTransferViewModel _fileTransferView;
        private string _friendName; //TODO: make this a binding to the friend view model (performace impact?)
        //private string _time; //making this a datetime object is probably a very bad idea
        //private ToxMessageType _type;

        public int FriendNumber { get; set; }

        public FileTransferMessageViewModel(int friendNumber)
        {
            FriendNumber = friendNumber;
        }

        public string FriendName
        {
            get { return _friendName; }
            set { _friendName = value; }
        }

        private string _time;
        public string Time
        {
            get { return _time; }
            set { _time = value; }
        }

        public FileTransferViewModel FileTransferView
        {
            get { return _fileTransferView; }
            set { _fileTransferView = value; }
        }
    }
}
