using SharpTox.Core;
using System.Windows.Media;
using Toxy.Managers;

namespace Toxy.ViewModels
{
    public interface IChatObject
    {
        string Name { get; set; }
        string StatusMessage { get; set; }
        ToxUserStatus UserStatus { get; set; }
        ToxConnectionStatus ConnectionStatus { get; set; }
        int ChatNumber { get; set; }
        bool IsSelected { get; set; }
        ConversationViewModel ConversationView { get; set; }
        bool IsOnline { get; set; }
        ImageSource Avatar { get; set; }
        bool HasUnreadMessages { get; set; }
        CallState CallState { get; set; }
        bool IsInVideoCall { get; set; }
        bool IsReceivingVideo { get; set; }
        bool IsTyping { get; set; }
    }
}
