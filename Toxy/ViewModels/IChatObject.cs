using System.Windows.Media;

using SharpTox.Core;
using Toxy.Common;

namespace Toxy.ViewModels
{
    public interface IChatObject
    {
        string Name { get; set; }
        int ChatNumber { get; set; }
        bool Selected { get; set; }
        bool HasNewMessage { get; set; }
        int NewMessageCount { get; set; }
        string StatusMessage { get; set; }
        string AdditionalInfo { get; set; }
        ToxStatus ToxStatus { get; set; }
        ImageSource Avatar { get; set; }
        byte[] AvatarBytes { get; set; }
    }
}