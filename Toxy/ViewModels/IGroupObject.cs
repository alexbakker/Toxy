using System;

namespace Toxy.ViewModels
{
    public interface IGroupObject : IChatObject
    {
        Action<IGroupObject, bool> SelectedAction { get; set; }
        Action<IGroupObject> DeleteAction { get; set; }
    }
}