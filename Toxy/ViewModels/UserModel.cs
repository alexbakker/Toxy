using System.Windows.Media;

using SharpTox.Core;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class UserModel : ViewModelBase, IUserObject
    {
        private string name;

        public string Name
        {
            get { return this.name; }
            set
            {
                if (!Equals(value, this.Name))
                {
                    this.name = value;
                    this.OnPropertyChanged(() => this.Name);
                }
            }
        }

        private int chatNumber;

        public int ChatNumber
        {
            get { return this.chatNumber; }
            set
            {
                if (!Equals(value, this.ChatNumber))
                {
                    this.chatNumber = value;
                    this.OnPropertyChanged(() => this.ChatNumber);
                }
            }
        }

        private ToxUserStatus toxStatus;

        public ToxUserStatus ToxStatus
        {
            get { return this.toxStatus; }
            set
            {
                if (!Equals(value, this.ToxStatus))
                {
                    this.toxStatus = value;
                    this.OnPropertyChanged(() => this.ToxStatus);
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

        private ImageSource avatar;

        public ImageSource Avatar
        {
            get { return this.avatar; }
            set
            {
                if (!Equals(value, this.Avatar))
                {
                    this.avatar = value;
                    this.OnPropertyChanged(() => this.Avatar);
                }
            }
        }
    }
}