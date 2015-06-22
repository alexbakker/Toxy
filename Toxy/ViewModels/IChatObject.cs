using SharpTox.Core;
using System.Windows.Media;

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
        void ChangeCallState(SharpTox.Av.ToxAvCallState toxAvCallState);
        bool IsCalling { get; set; }
        bool IsRinging { get; set; }
        bool IsCallInProgress { get; set; }
        bool IsInVideoCall { get; set; }
    }
}
