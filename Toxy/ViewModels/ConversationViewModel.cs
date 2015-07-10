using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using Toxy.MVVM;
using Toxy.Views;
using System.Windows.Media;
using Toxy.Windows;
using Toxy.Managers;
using Toxy.Extensions;

namespace Toxy.ViewModels
{
    public class ConversationViewModel : ViewModelBase, IConversationView
    {
        public FriendControlViewModel Friend { get; private set; }

        private ObservableCollection<ViewModelBase> _messages = new ObservableCollection<ViewModelBase>();
        public ObservableCollection<ViewModelBase> Messages
        {
            get { return _messages; }
            set
            {
                if (Equals(value, _messages))
                {
                    return;
                }
                _messages = value;
                OnPropertyChanged(() => Messages);
            }
        }

        private string _enteredText;
        public string EnteredText
        {
            get { return _enteredText; }
            set
            {
                if (Equals(value, _enteredText))
                {
                    return;
                }
                _enteredText = value;
                OnPropertyChanged(() => EnteredText);
            }
        }

        private ImageSource _currentFrame;
        public ImageSource CurrentFrame
        {
            get { return _currentFrame; }
            set
            {
                if (Equals(value, _currentFrame))
                {
                    return;
                }
                _currentFrame = value;
                OnPropertyChanged(() => CurrentFrame);
            }
        }

        public ConversationWindow Window { get; set; }

        public ConversationViewModel(FriendControlViewModel model)
        {
            Friend = model;
        }

        public void AddMessage(MessageViewModel message)
        {
            var lastMessage = _messages.LastOrDefault(m => m is MessageViewModel);

            if (lastMessage != null && (lastMessage as MessageViewModel).FriendNumber == message.FriendNumber)
                message.FriendName = string.Empty;

            Messages.Add(message);
        }

        public void AddTransfer(FileTransfer transfer)
        {
            var transferModel = new FileTransferViewModel(transfer.FriendNumber, transfer);
            transferModel.Name = transfer.Name;
            transferModel.Size = transfer.Size.GetSizeString();

            var viewModel = new FileTransferMessageViewModel(transfer.FriendNumber);
            viewModel.FriendName = ProfileManager.Instance.Tox.GetFriendName(transfer.FriendNumber);
            viewModel.Time = DateTime.Now.ToShortTimeString();
            viewModel.FileTransferView = transferModel;

            Messages.Add(viewModel);
        }
    }
}
