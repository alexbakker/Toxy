using System;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using SharpTox.Core;

using Toxy.Common;
using Toxy.MVVM;
using Win32;

namespace Toxy.ViewModels
{
    public class FriendControlModelView : BaseChatModelView, IFriendObject
    {
        public FriendControlModelView(MainWindowViewModel mainViewModel)
        {
            this.MainViewModel = mainViewModel;
        }

        public MainWindowViewModel MainViewModel { get; set; }

        public MessageData RequestMessageData { get; set; }
        public FlowDocument RequestFlowDocument { get; set; }

        public Action<IFriendObject, bool> SelectedAction { get; set; }
        public Action<IFriendObject> DeleteAction { get; set; }
        public Action<IFriendObject> CopyIDAction { get; set; }
        public Action<IFriendObject, IGroupObject> GroupInviteAction { get; set; }

        public Action<IFriendObject> AcceptAction { get; set; }
        public Action<IFriendObject> DeclineAction { get; set; }

        public Action<IFriendObject> AcceptCallAction { get; set; }
        public Action<IFriendObject> DenyCallAction { get; set; }

        public Action<IFriendObject> HangupAction { get; set; }

        private ICommand deleteCommand;

        public ICommand DeleteCommand
        {
            get { return this.deleteCommand ?? (this.deleteCommand = new DelegateCommand(() => this.DeleteAction(this), () => DeleteAction != null)); }
        }

        private ICommand copyIDCommand;

        public ICommand CopyIDCommand
        {
            get { return this.copyIDCommand ?? (this.copyIDCommand = new DelegateCommand(() => this.CopyIDAction(this), () => CopyIDAction != null)); }
        }

        private ICommand acceptCommand;

        public ICommand AcceptCommand
        {
            get
            {
                return this.acceptCommand ?? (this.acceptCommand = new DelegateCommand(() 
                    => this.AcceptAction(this), () =>
                    {
                        Winmm.StopCallingNotify();
                        return IsRequest && AcceptAction != null;
                    }
                ));
            }
        }

        private ICommand declineCommand;

        public ICommand DeclineCommand
        {
            get { return this.declineCommand ?? (this.declineCommand = new DelegateCommand(() 
                => this.DeclineAction(this), () =>
                {
                    Winmm.StopCallingNotify();
                    return IsRequest && this.DeclineAction != null;
                }
            )); }
        }

        private ICommand acceptCallCommand;

        public ICommand AcceptCallCommand
        {
            get { return this.acceptCallCommand ?? (this.acceptCallCommand = new DelegateCommand(() => this.AcceptCallAction(this), () => IsCalling && AcceptCallAction != null)); }
        }

        private ICommand denyCallCommand;

        public ICommand DenyCallCommand
        {
            get
            {
                Winmm.StopCallingNotify();
                return this.denyCallCommand ?? (this.denyCallCommand = new DelegateCommand(() => this.DenyCallAction(this), () => IsCalling && this.DenyCallAction != null));
            }
        }

        private ICommand groupInviteCommand;

        public ICommand GroupInviteCommand
        {
            get
            {
                return this.groupInviteCommand ?? (this.groupInviteCommand = new DelegateCommand<IGroupObject>((go) => this.GroupInviteAction(this, go), (go) => this.ToxStatus == ToxStatus.None && GroupInviteAction != null && go != null));
            }
        }

        private ICommand hangupCommand;

        public ICommand HangupCommand
        {
            get { return this.hangupCommand ?? (this.hangupCommand = new DelegateCommand(() => this.HangupAction(this), () => this.HangupAction != null)); }
        }

        private bool selected;

        public override bool Selected
        {
            get { return this.selected; }
            set
            {
                if (!Equals(value, this.Selected))
                {
                    this.selected = value;
                    this.OnPropertyChanged(() => this.Selected);
                }
                if (!value)
                {
                    this.HasNewMessage = false;
                }
                var action = this.SelectedAction;
                if (action != null)
                {
                    action(this, value);
                }
            }
        }

        private bool isRequest;

        public bool IsRequest
        {
            get { return this.isRequest; }
            set
            {
                if (!Equals(value, this.IsRequest))
                {
                    this.isRequest = value;
                    this.OnPropertyChanged(() => this.IsRequest);
                }
            }
        }

        private bool isCalling;

        public bool IsCalling
        {
            get { return this.isCalling; }
            set
            {
                if (!Equals(value, this.IsCalling))
                {
                    this.isCalling = value;
                    this.OnPropertyChanged(() => this.IsCalling);
                }
            }
        }

        private bool isCallingToFriend;

        public bool IsCallingToFriend
        {
            get { return this.isCallingToFriend; }
            set
            {
                if (!Equals(value, this.IsCallingToFriend))
                {
                    this.isCallingToFriend = value;
                    this.OnPropertyChanged(() => this.IsCallingToFriend);
                }
            }
        }

        public int CallIndex { get; set; }
    }
}
