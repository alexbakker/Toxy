using System;
using System.ComponentModel;

using SharpTox.Core;
using Toxy.MVVM;

namespace Toxy.Common
{
    public class GroupPeer : ViewModelBase
    {
        public ToxKey PublicKey { get; private set; }
        public int GroupNumber { get; private set; }

        public GroupPeer(int groupNumber, ToxKey publicKey)
        {
            GroupNumber = groupNumber;
            PublicKey = publicKey;
        }

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

        private bool muted;

        public bool Muted
        {
            get { return this.muted; }
            set
            {
                if (!Equals(value, this.Muted))
                {
                    this.muted = value;
                    this.OnPropertyChanged(() => this.Muted);
                }
            }
        }

        private bool ignored;

        public bool Ignored
        {
            get { return this.ignored; }
            set
            {
                if (!Equals(value, this.Ignored))
                {
                    this.ignored = value;
                    this.OnPropertyChanged(() => this.Ignored);
                }
            }
        }
    }
}
