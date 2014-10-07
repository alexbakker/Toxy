using System;
using System.Windows.Input;
using System.Windows.Media;

using SharpTox.Core;

using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class GroupControlModelView : BaseChatModelView, IGroupObject
    {
        public Action<IGroupObject, bool> SelectedAction { get; set; }
        public Action<IGroupObject> DeleteAction { get; set; }

        protected ICommand deleteCommand;

        public ICommand DeleteCommand
        {
            get { return this.deleteCommand ?? (this.deleteCommand = new DelegateCommand(() => this.DeleteAction(this), () => DeleteAction != null)); }
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
    }
}