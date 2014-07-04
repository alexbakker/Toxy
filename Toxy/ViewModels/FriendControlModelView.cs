using System;
using System.Windows.Documents;
using System.Windows.Input;
using SharpTox.Core;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class FriendControlModelView : ViewModelBase, IFriendObject
    {
        public MessageData RequestMessageData { get; set; }
        public FlowDocument RequestFlowDocument { get; set; }

        public Action<IFriendObject, bool> SelectedAction { get; set; }

        public Action<IFriendObject> AcceptAction { get; set; }
        public Action<IFriendObject> DeclineAction { get; set; }

        public Action<IFriendObject> AcceptCallAction { get; set; }
        public Action<IFriendObject> DenyCallAction { get; set; }

        private ICommand acceptCommand;

        public ICommand AcceptCommand
        {
            get { return this.acceptCommand ?? (this.acceptCommand = new DelegateCommand(() => this.AcceptAction(this), () => IsRequest && AcceptAction != null)); }
        }

        private ICommand declineCommand;

        public ICommand DeclineCommand
        {
            get { return this.declineCommand ?? (this.declineCommand = new DelegateCommand(() => this.DeclineAction(this), () => IsRequest && this.DeclineAction != null)); }
        }

        private ICommand acceptCallCommand;

        public ICommand AcceptCallCommand
        {
            get { return this.acceptCallCommand ?? (this.acceptCallCommand = new DelegateCommand(() => this.AcceptCallAction(this), () => IsCalling && AcceptCallAction != null)); }
        }

        private ICommand denyCallCommand;

        public ICommand DenyCallCommand
        {
            get { return this.denyCallCommand ?? (this.denyCallCommand = new DelegateCommand(() => this.DenyCallAction(this), () => IsCalling && this.DenyCallAction != null)); }
        }

        private bool selected;

        public bool Selected
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

        private int friendNumber;

        public int FriendNumber
        {
            get { return this.friendNumber; }
            set
            {
                if (!Equals(value, this.FriendNumber))
                {
                    this.friendNumber = value;
                    this.OnPropertyChanged(() => this.FriendNumber);
                }
            }
        }

        private ToxUserStatus userStatus;

        public ToxUserStatus UserStatus
        {
            get { return this.userStatus; }
            set
            {
                if (!Equals(value, this.UserStatus))
                {
                    this.userStatus = value;
                    this.OnPropertyChanged(() => this.UserStatus);
                }
            }
        }

        private string statusMessage;

        public string StatusMessage
        {
            get { return this.statusMessage; }
            set
            {
                if (!Equals(value, this.StatusMessage))
                {
                    this.statusMessage = value;
                    this.OnPropertyChanged(() => this.StatusMessage);
                }
            }
        }

        private string userName;

        public string UserName
        {
            get { return this.userName; }
            set
            {
                if (!Equals(value, this.UserName))
                {
                    this.userName = value;
                    this.OnPropertyChanged(() => this.UserName);
                }
            }
        }

        private bool hasNewMessage;

        public bool HasNewMessage
        {
            get { return this.hasNewMessage; }
            set
            {
                if (!Equals(value, this.HasNewMessage))
                {
                    this.hasNewMessage = value;
                    this.OnPropertyChanged(() => this.HasNewMessage);
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
    }
}
