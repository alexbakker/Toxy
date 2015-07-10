using SharpTox.Core;
using System.Windows.Media;
using Toxy.Managers;

namespace Toxy.ViewModels
{
    public interface IChatObject
    {
        string Name { get; set; }
        string StatusMessage { get; set; }
        int ChatNumber { get; set; }
        bool IsSelected { get; set; }
        IConversationView ConversationView { get; set; }
        bool HasUnreadMessages { get; set; }
    }
}
