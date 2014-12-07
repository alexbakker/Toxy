using System;
using System.Linq;
using System.Collections.ObjectModel;

using SharpTox.Core;

namespace Toxy.Common
{
    public class GroupPeerCollection : ObservableCollection<GroupPeer>
    {
        public bool ContainsPeer(ToxKey publicKey)
        {
            return GetPeerByPublicKey(publicKey) != null;
        }

        public void RemovePeer(ToxKey publicKey)
        {
            var peer = GetPeerByPublicKey(publicKey);

            if (peer != null)
                this.Remove(peer);
        }

        public GroupPeer GetPeerByPublicKey(ToxKey publicKey)
        {
            var peers = this.Where(p => p.PublicKey == publicKey).ToArray();

            if (peers.Length == 1)
                return peers[0];
            else
                return null;
        }
    }
}
