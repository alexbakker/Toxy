using System.Collections.ObjectModel;
using Toxy.MVVM;
using Toxy.Windows;

namespace Toxy.ViewModels
{
    public interface IConversationView : IView
    {
        ObservableCollection<ViewModelBase> Messages { get; set; }
        string EnteredText { get; set; }
        ConversationWindow Window { get; set; }
        IChatObject ChatObject { get; }
    }
}
