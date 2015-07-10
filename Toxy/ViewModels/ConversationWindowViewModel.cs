using System;

namespace Toxy.ViewModels
{
    public class ConversationWindowViewModel
    {
        public IConversationView CurrentView { get; private set; }

        public ConversationWindowViewModel(IConversationView model)
        {
            CurrentView = model;
        }
    }
}
