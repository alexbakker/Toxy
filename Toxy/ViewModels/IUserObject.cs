using SharpTox.Core;
using Toxy.Common;

namespace Toxy.ViewModels
{
    public interface IUserObject
    {
        string Name { get; set; }
        int ChatNumber { get; set; }
        ToxStatus ToxStatus { get; set; }
        string StatusMessage { get; set; }
    }
}