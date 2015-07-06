using System;
using System.Timers;
using Toxy.Managers;
using Toxy.MVVM;
using Toxy.Extensions;

namespace Toxy.ViewModels
{
    public class FileTransferViewModel : ViewModelBase
    {
        public int FriendNumber { get; set; }

        private Timer _timer;
        private long _lastReceiveCount;

        public FileTransferViewModel(int friendNumber, FileTransfer transfer)
        {
            FriendNumber = friendNumber;
            Transfer = transfer;
            Transfer.OnStopped += transfer_OnStopped;
            Transfer.OnStarted += transfer_OnStarted;

            _timer = new Timer(500d);
            _timer.Elapsed += timer_Elapsed;
        }

        private void transfer_OnStarted(object sender, EventArgs e)
        {
            _timer.Start();
            IsInProgress = true;
        }

        private void transfer_OnStopped(object sender, EventArgs e)
        {
            IsInProgress = false;
            IsFinished = true;

            _timer.Dispose();
            timer_Elapsed(null, null);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //TODO: refactor
            Progress = (int)(((double)Transfer.TransferredBytes / Transfer.Size) * 100);
            Speed = ((Transfer.TransferredBytes - _lastReceiveCount) * 2).GetSizeString() + "/s";

            if (_lastReceiveCount != 0 && Transfer.TransferredBytes != 0)
            {
                //catch exception in the event that the timespan is too long
                try { TimeLeft = TimeSpan.FromSeconds((double)(Transfer.Size - Transfer.TransferredBytes) / ((Transfer.TransferredBytes - _lastReceiveCount) * 2)).ToString("h'h 'm'm 's's'"); }
                catch { }
            }

            _lastReceiveCount = Transfer.TransferredBytes;
        }

        public FileTransfer Transfer { get; private set; }

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

        private string _speed;
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

        private string _timeLeft;
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

        private int _progress;
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

        private bool _isFinished;
        public bool IsFinished
        {
            get { return _isFinished; }
            set
            {
                if (Equals(value, _isFinished))
                {
                    return;
                }
                _isFinished = value;
                OnPropertyChanged(() => IsFinished);
            }
        }

        private bool _isInProgress;
        public bool IsInProgress
        {
            get { return _isInProgress; }
            set
            {
                if (Equals(value, _isInProgress))
                {
                    return;
                }
                _isInProgress = value;
                OnPropertyChanged(() => IsInProgress);
            }
        }

        private string _error;
        public string Error
        {
            get { return _error; }
            set
            {
                if (Equals(value, _error))
                {
                    return;
                }
                _error = value;
                OnPropertyChanged(() => Error);
            }
        }

        public FileTransferDirection Direction
        {
            get { return Transfer.Direction; }
        }
    }
}
