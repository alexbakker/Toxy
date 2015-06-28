using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using Toxy.MVVM;
using Toxy.Views;
using System.Windows.Media;
using Toxy.Windows;

namespace Toxy.ViewModels
{
    public class ConversationViewModel : ViewModelBase
    {
        public FriendControlViewModel Friend { get; private set; }

        private ObservableCollection<IMessage> _messages = new ObservableCollection<IMessage>();
        public ObservableCollection<IMessage> Messages
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
            var lastMessage = _messages.LastOrDefault();

            if (lastMessage != null && lastMessage.FriendNumber == message.FriendNumber)
                message.FriendName = string.Empty;

            Messages.Add(message);
        }
    }
}
