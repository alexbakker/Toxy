using System;

namespace Toxy.ViewModels
{
    public class ConversationWindowViewModel
    {
        public ConversationViewModel CurrentView { get; private set; }

        public ConversationWindowViewModel(ConversationViewModel model)
        {
            CurrentView = model;
        }
    }
}
