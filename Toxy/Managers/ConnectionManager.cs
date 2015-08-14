using System;
using SharpTox.Core;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Toxy.Managers
{
    public class ConnectionManager
    {
        private static ConnectionManager _instance;

        private ConnectionManager()
        {
            ProfileManager.Instance.Tox.OnConnectionStatusChanged += Tox_OnConnectionStatusChanged;
            ProfileManager.Instance.Tox.OnFriendConnectionStatusChanged += Tox_OnFriendConnectionStatusChanged;
        }

        private void Tox_OnFriendConnectionStatusChanged(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            Debugging.Write(string.Format("Friend {0} connnection status changed to: {1}", ProfileManager.Instance.Tox.GetFriendName(e.FriendNumber), e.Status));
        }

        public static ConnectionManager Get()
        {
            if (_instance == null)
                _instance = new ConnectionManager();

            return _instance;
        }

        private void Tox_OnConnectionStatusChanged(object sender, ToxEventArgs.ConnectionStatusEventArgs e)
        {
            if (e.Status == ToxConnectionStatus.None)
            {
                WaitAndBootstrap(2000);
            }

            Debugging.Write("Connection status changed to: " + e.Status);
        }

        private async void WaitAndBootstrap(int delay)
        {
            await Task.Factory.StartNew(async () =>
            {
                //wait 'delay' seconds, check if we're connected, if not, bootstrap again
                await Task.Delay(delay);

                if (!ProfileManager.Instance.Tox.IsConnected)
                {
                    Debugging.Write("We're still not connected, bootstrapping again");
                    DoBootstrap();
                }
            });
        }

        public void DoBootstrap()
        {
            var nodes = Config.Instance.Nodes;

            if (nodes.Length >= 4)
            {
                var random = new Random();
                var indices = new List<int>();

                for (int i = 0; i < 4; )
                {
                    int index = random.Next(nodes.Length);
                    if (indices.Contains(index))
                        continue;

                    var node = nodes[index];
                    if (Bootstrap(nodes[index]))
                    {
                        indices.Add(index);
                        i++;
                    }
                }
            }
            else
            {
                foreach (var node in nodes)
                    Bootstrap(node);
            }

            WaitAndBootstrap(20000);
        }

        private bool Bootstrap(ToxConfigNode node)
        {
            var toxNode = new ToxNode(node.Address, node.Port, new ToxKey(ToxKeyType.Public, node.PublicKey));
            var error = ToxErrorBootstrap.Ok;
            bool success = ProfileManager.Instance.Tox.Bootstrap(toxNode, out error);

            if (success)
                Debugging.Write(string.Format("Bootstrapped off of {0}:{1}", node.Address, node.Port));
            else
                Debugging.Write(string.Format("Could not bootstrap off of {0}:{1}, error: {2}", node.Address, node.Port, error));

            //even if adding the tcp relay fails for some reason (while it shouldn't...), we'll consider this successful.
            if (ProfileManager.Instance.Tox.AddTcpRelay(toxNode, out error))
                Debugging.Write(string.Format("Added TCP relay {0}:{1}", node.Address, node.Port));
            else
                Debugging.Write(string.Format("Could not add TCP relay {0}:{1}, error: {2}", node.Address, node.Port, error));

            return success;
        }
    }
}
