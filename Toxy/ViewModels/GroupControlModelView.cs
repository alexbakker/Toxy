using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

using SharpTox.Core;

using Toxy.MVVM;
using Toxy.Common;

namespace Toxy.ViewModels
{
    public class GroupControlModelView : BaseChatModelView, IGroupObject
    {
        public Action<IGroupObject, bool> SelectedAction { get; set; }
        public Action<IGroupObject> DeleteAction { get; set; }
        public Action<IGroupObject> ChangeTitleAction { get; set; }

        protected ICommand deleteCommand;

        public ICommand DeleteCommand
        {
            get { return this.deleteCommand ?? (this.deleteCommand = new DelegateCommand(() => this.DeleteAction(this), () => DeleteAction != null)); }
        }

        protected ICommand changeTitleCommand;

        public ICommand ChangeTitleCommand
        {
            get { return this.changeTitleCommand ?? (this.changeTitleCommand = new DelegateCommand(() => this.ChangeTitleAction(this), () => ChangeTitleAction != null)); }
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

        private ToxGroupType groupType;

        public ToxGroupType GroupType
        {
            get { return this.groupType; }
            set
            {
                if (!Equals(value, this.GroupType))
                {
                    this.groupType = value;
                    this.OnPropertyChanged(() => this.GroupType);
                }
            }
        }


        private GroupPeerCollection peerList = new GroupPeerCollection();

        public GroupPeerCollection PeerList
        {
            get { return this.peerList; }
            set
            {
                if (!Equals(value, this.PeerList))
                {
                    this.peerList = value;
                    this.OnPropertyChanged(() => this.PeerList);
                }
            }
        }
    }
}
