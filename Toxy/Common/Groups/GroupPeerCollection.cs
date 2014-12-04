using System;
using System.Linq;
using System.Collections.Generic;

namespace Toxy.Common
{
    public class GroupPeerCollection : List<GroupPeer>
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
            return this.Find(p => p.PeerNumber == peerNumber);
        }
    }
}
