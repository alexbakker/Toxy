using System;

namespace Toxy.Common
{
    [Serializable]
    public class Config
    {
        private bool udpDisabled = false;

        public bool UdpDisabled
        {
            get { return udpDisabled; }
            set { udpDisabled = value; }
        }

        private bool ipv6Enabled = true;

        public bool Ipv6Enabled
        {
            get { return ipv6Enabled; }
            set { ipv6Enabled = value; }
        }

        private bool proxyEnabled = false;

        public bool ProxyEnabled
        {
            get { return proxyEnabled; }
            set { proxyEnabled = value; }
        }

        private string proxyAddress = "";

        public string ProxyAddress
        {
            get { return proxyAddress; }
            set { proxyAddress = value; }
        }

        private int proxyPort = 0;

        public int ProxyPort
        {
            get { return proxyPort; }
            set { proxyPort = value; }
        }

        private bool hideInTray = false;

        public bool HideInTray
        {
            get { return hideInTray; }
            set { hideInTray = value; }
        }

        private int outputDevice;

        public int OutputDevice
        {
            get { return outputDevice; }
            set { outputDevice = value; }
        }

        private int inputDevice;

        public int InputDevice
        {
            get { return inputDevice; }
            set { inputDevice = value; }
        }

        private string accentColor;

        public string AccentColor
        {
            get { return accentColor; }
            set { accentColor = value; }
        }

        private string theme;

        public string Theme
        {
            get { return theme; }
            set { theme = value; }
        }
    }
}
