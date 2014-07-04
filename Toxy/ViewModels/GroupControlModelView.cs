using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class GroupControlModelView : ViewModelBase, IGroupObject
    {
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
    }
}