using System.Windows.Documents;
using SharpTox.Core;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class FriendControlModelView : ViewModelBase, IChatObject
    {
        public FlowDocument RequestFlowDocument { get; set; }

        private bool selected;

        public bool Selected
        {
            get { return this.selected; }
            set
            {
                if (Equals(value, this.selected))
                {
                    return;
                }
                this.selected = value;
                this.OnPropertyChanged(() => this.Selected);
            }
        }

        private int friendNumber;

        public int FriendNumber
        {
            get { return this.friendNumber; }
            set
            {
                if (Equals(value, this.friendNumber))
                {
                    return;
                }
                this.friendNumber = value;
                this.OnPropertyChanged(() => this.FriendNumber);
            }
        }

        private ToxUserStatus userStatus;

        public ToxUserStatus UserStatus
        {
            get { return this.userStatus; }
            set
            {
                if (Equals(value, this.userStatus))
                {
                    return;
                }
                this.userStatus = value;
                this.OnPropertyChanged(() => this.UserStatus);
            }
        }

        private string statusMessage;

        public string StatusMessage
        {
            get { return this.statusMessage; }
            set
            {
                if (Equals(value, this.statusMessage))
                {
                    return;
                }
                this.statusMessage = value;
                this.OnPropertyChanged(() => this.StatusMessage);
            }
        }

        private string userName;

        public string UserName
        {
            get { return this.userName; }
            set
            {
                if (Equals(value, this.userName))
                {
                    return;
                }
                this.userName = value;
                this.OnPropertyChanged(() => this.UserName);
            }
        }
    }
}
