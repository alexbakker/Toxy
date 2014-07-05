using System;
using SharpTox.Core;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class GroupControlModelView : ViewModelBase, IGroupObject
    {
        public Action<IGroupObject, bool> SelectedAction { get; set; }

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

        private int groupNumber;

        public int GroupNumber
        {
            get { return this.groupNumber; }
            set
            {
                if (!Equals(value, this.GroupNumber))
                {
                    this.groupNumber = value;
                    this.OnPropertyChanged(() => this.GroupNumber);
                }
            }
        }

        private string groupName;

        public string GroupName
        {
            get { return this.groupName; }
            set
            {
                if (!Equals(value, this.GroupName))
                {
                    this.groupName = value;
                    this.OnPropertyChanged(() => this.GroupName);
                }
            }
        }

        private ToxUserStatus groupStatus;

        public ToxUserStatus GroupStatus
        {
            get { return this.groupStatus; }
            set
            {
                if (!Equals(value, this.GroupStatus))
                {
                    this.groupStatus = value;
                    this.OnPropertyChanged(() => this.GroupStatus);
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