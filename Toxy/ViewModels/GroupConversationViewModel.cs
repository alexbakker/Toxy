using System.Linq;
using System.Collections.ObjectModel;
using Toxy.MVVM;
using Toxy.Windows;
using System;

namespace Toxy.ViewModels
{
    public class GroupConversationViewModel : ViewModelBase, IConversationView
    {
        public GroupControlViewModel Group { get; private set; }

        public IChatObject ChatObject
        {
            get { return Group; }
        }

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

        public ConversationWindow Window { get; set; }

        public GroupConversationViewModel(GroupControlViewModel model)
        {
            Group = model;
        }

        public void AddMessage(MessageViewModel message)
        {
            var lastMessage = _messages.LastOrDefault(m => m is MessageViewModel);

            if (lastMessage != null && (lastMessage as MessageViewModel).FriendNumber == message.FriendNumber)
                message.FriendName = string.Empty;

            Messages.Add(message);
        }
    }
}
