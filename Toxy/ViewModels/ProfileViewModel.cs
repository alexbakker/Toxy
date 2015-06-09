using SharpTox.Core;
using System;
using Toxy.MVVM;
using Toxy.Tools;

namespace Toxy.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        public string Name { get; set; }
        public string StatusMessage { get; set; }
        public ToxUserStatus UserStatus { get; set; }
        public ToxId ToxId { get; set; }

        public ProfileViewModel(ToxSave profile)
        {
            Name = profile.Name;
            UserStatus = profile.Status;
            StatusMessage = profile.StatusMessage;
            ToxId = profile.Id;
        }
    }
}
