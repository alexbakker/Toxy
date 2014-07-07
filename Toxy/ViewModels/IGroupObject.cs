using System;
using SharpTox.Core;

namespace Toxy.ViewModels
{
    public interface IGroupObject : IChatObject
    {
        Action<IGroupObject, bool> SelectedAction { get; set; }
        Action<IGroupObject> DeleteAction { get; set; }

        int ChatNumber { get; set; }
        ToxUserStatus GroupStatus { get; set; }
    }
}