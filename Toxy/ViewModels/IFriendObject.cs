using SharpTox.Core;
using System.Windows.Media;
using Toxy.Managers;

namespace Toxy.ViewModels
{
    public interface IFriendObject : IChatObject
    {
        ToxUserStatus UserStatus { get; set; }
        ToxConnectionStatus ConnectionStatus { get; set; }
        bool IsOnline { get; set; }
        ImageSource Avatar { get; set; }
        CallState CallState { get; set; }
        bool IsInVideoCall { get; set; }
        bool IsReceivingVideo { get; set; }
        bool IsTyping { get; set; }
    }
}
