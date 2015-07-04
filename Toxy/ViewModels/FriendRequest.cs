using System;

namespace Toxy.ViewModels
{
    public class FriendRequest
    {
        public string PublicKey { get; private set; }
        public string Message { get; private set; }

        public FriendRequest(string publicKey, string message)
        {
            PublicKey = publicKey;
            Message = message;
        }
    }
}
