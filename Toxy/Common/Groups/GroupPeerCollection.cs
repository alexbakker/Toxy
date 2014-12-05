using System;
using System.Linq;
using System.Collections.ObjectModel;

namespace Toxy.Common
{
    public class GroupPeerCollection : ObservableCollection<GroupPeer>
    {
        public bool ContainsPeer(int peerNumber)
        {
            return GetPeerByNumber(peerNumber) != null;
        }

        public void RemovePeer(int peerNumber)
        {
            var peer = GetPeerByNumber(peerNumber);

            if (peer != null)
                this.Remove(peer);
        }

        public GroupPeer GetPeerByNumber(int peerNumber)
        {
            var peers = this.Where(p => p.PeerNumber == peerNumber).ToArray();

            if (peers.Length == 1)
                return peers[0];
            else
                return null;
        }
    }
}
