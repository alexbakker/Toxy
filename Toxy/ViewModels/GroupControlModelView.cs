using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class GroupControlModelView : ViewModelBase, IChatObject
    {
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
    }
}