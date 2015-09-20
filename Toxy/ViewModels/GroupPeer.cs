using SharpTox.Core;
using System;

namespace Toxy.ViewModels
{
    public class GroupPeer
    {
        public string Name { get; set; }
        public int PeerNumber { get; set; }
        public ToxKey PublicKey { get; private set; }

        public bool Ignored { get; set; }

        public GroupPeer(int peerNumber, ToxKey publicKey)
        {
            PeerNumber = peerNumber;
            PublicKey = publicKey;
            Name = "Unknown";
        }
    }
}
