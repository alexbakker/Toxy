using SharpTox.Core;
using System.Collections.ObjectModel;
namespace Toxy.ViewModels
{
    public interface IGroupObject : IChatObject
    {
        ObservableCollection<GroupPeer> Peers { get; set; }
        GroupPeer FindPeer(ToxKey key);
    }
}
