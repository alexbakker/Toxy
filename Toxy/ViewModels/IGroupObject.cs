using System;
using System.Collections.Generic;

using Toxy.Common;

namespace Toxy.ViewModels
{
    public interface IGroupObject : IChatObject
    {
        Action<IGroupObject, bool> SelectedAction { get; set; }
        Action<IGroupObject> DeleteAction { get; set; }
        Action<IGroupObject> ChangeTitleAction { get; set; }
        GroupPeerCollection PeerList { get; set; }
    }
}