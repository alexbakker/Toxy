using SharpTox.Core;

namespace Toxy.ViewModels
{
    public interface IUserObject
    {
        string Name { get; set; }
        int ChatNumber { get; set; }
        ToxUserStatus ToxStatus { get; set; }
        string StatusMessage { get; set; }
    }
}