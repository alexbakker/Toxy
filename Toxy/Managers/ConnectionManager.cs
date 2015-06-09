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
            App.Tox.OnConnectionStatusChanged += Tox_OnConnectionStatusChanged;
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

                if (!App.Tox.IsConnected)
                {
                    Debugging.Write("We're still not connected, bootstrapping again");
                    DoBootstrap();
                }
            });
        }

        public void DoBootstrap()
        {
            if (_nodes.Length >= 4)
            {
                var random = new Random();
                var indices = new List<int>();

                for (int i = 0; i < 4; )
                {
                    int index = random.Next(_nodes.Length);
                    if (indices.Contains(index))
                        continue;

                    var node = _nodes[index];
                    if (Bootstrap(_nodes[index]))
                    {
                        indices.Add(index);
                        i++;
                    }
                }
            }
            else
            {
                foreach (var node in _nodes)
                    Bootstrap(node);
            }

            WaitAndBootstrap(20000);
        }

        private bool Bootstrap(ToxNode node)
        {
            var error = ToxErrorBootstrap.Ok;
            bool success = App.Tox.Bootstrap(node, out error);
            if (success)
                Debugging.Write(string.Format("Bootstrapped off of {0}:{1}", node.Address, node.Port));
            else
                Debugging.Write(string.Format("Could not bootstrap off of {0}:{1}, error: {2}", node.Address, node.Port, error));

            return success;
        }

        private static ToxNode[] _nodes = new ToxNode[]
        {
            new ToxNode("178.62.250.138", 33445, new ToxKey(ToxKeyType.Public, "788236D34978D1D5BD822F0A5BEBD2C53C64CC31CD3149350EE27D4D9A2F9B6B")),
            new ToxNode("192.210.149.121", 33445, new ToxKey(ToxKeyType.Public, "F404ABAA1C99A9D37D61AB54898F56793E1DEF8BD46B1038B9D822E8460FAB67")),
            new ToxNode("178.62.125.224", 33445, new ToxKey(ToxKeyType.Public, "10B20C49ACBD968D7C80F2E8438F92EA51F189F4E70CFBBB2C2C8C799E97F03E")),
            new ToxNode("76.191.23.96", 33445, new ToxKey(ToxKeyType.Public, "93574A3FAB7D612FEA29FD8D67D3DD10DFD07A075A5D62E8AF3DD9F5D0932E11")),
        };
    }
}
