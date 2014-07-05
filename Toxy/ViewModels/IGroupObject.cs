using System;
using SharpTox.Core;

namespace Toxy.ViewModels
{
    public interface IGroupObject : IChatObject
    {
        Action<IGroupObject, bool> SelectedAction { get; set; }

        int ChatNumber { get; set; }
        string GroupName { get; set; }
        ToxUserStatus GroupStatus { get; set; }
        string StatusMessage { get; set; }
    }
}