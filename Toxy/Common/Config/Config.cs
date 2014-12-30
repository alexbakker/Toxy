using System;
using SharpTox.Core;

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

        private bool portable = false;

        public bool Portable
        {
            get { return portable; }
            set { portable = value; }
        }

        private bool enableaudionotifications = false;

        public bool EnableAudioNotifications
        {
            get { return enableaudionotifications; }
            set { enableaudionotifications = value; }
        }

        private bool alwaysNotify = false;

        public bool AlwaysNotify
        {
            get { return alwaysNotify; }
            set { alwaysNotify = value; }
        }

       private bool ipv6Enabled = true;

        public bool Ipv6Enabled
        {
            get { return ipv6Enabled; }
            set { ipv6Enabled = value; }
        }

        private bool remindAboutProxy = true;

        public bool RemindAboutProxy
        {
            get { return remindAboutProxy; }
            set { remindAboutProxy = value; }
        }

        private ToxProxyType proxyType = ToxProxyType.None;

        public ToxProxyType ProxyType
        {
            get { return proxyType; }
            set { proxyType = value; }
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

        private int outputDevice = 0;

        public int OutputDevice
        {
            get { return outputDevice; }
            set { outputDevice = value; }
        }

        private int inputDevice = 0;

        public int InputDevice
        {
            get { return inputDevice; }
            set { inputDevice = value; }
        }

        private string videoDevice = "";

        public string VideoDevice
        {
            get { return videoDevice; }
            set { videoDevice = value; }
        }

        private string accentColor = "Blue";

        public string AccentColor
        {
            get { return accentColor; }
            set { accentColor = value; }
        }

        private string theme = "BaseLight";

        public string Theme
        {
            get { return theme; }
            set { theme = value; }
        }

        private bool filterAudio = false;

        public bool FilterAudio
        {
            get { return filterAudio; }
            set { filterAudio = value; }
        }

        private string profileName = string.Empty;

        public string ProfileName
        {
            get { return profileName; }
            set { profileName = value; }
        }

        private bool enableChatLogging = false;

        public bool EnableChatLogging
        {
            get { return enableChatLogging; }
            set { enableChatLogging = value; }
        }

        private ToxConfigNode[] nodes = new ToxConfigNode[] 
        {
            new ToxConfigNode() { ClientId = "951C88B7E75C867418ACDB5D273821372BB5BD652740BCDF623A4FA293E75D2F", Address = "192.254.75.98", Port = 33445 },
            new ToxConfigNode() { ClientId = "04119E835DF3E78BACF0F84235B300546AF8B936F035185E2A8E9E0A67C8924F", Address = "144.76.60.215", Port = 33445 },
            new ToxConfigNode() { ClientId = "A09162D68618E742FFBCA1C2C70385E6679604B2D80EA6E84AD0996A1AC8A074", Address = "23.226.230.47", Port = 33445 }
        };

        public ToxConfigNode[] Nodes
        {
            get { return nodes; }
            set { nodes = value; }
        }

        private ToxNameService[] nameServices = new ToxNameService[]
        {
            new ToxNameService(){ Domain = "toxme.se", PublicKey = "5D72C517DF6AEC54F1E977A6B6F25914EA4CF7277A85027CD9F5196DF17E0B13", PublicKeyUrl = "" },
            new ToxNameService(){ Domain = "utox.org", PublicKey = "D3154F65D28A5B41A05D4AC7E4B39C6B1C233CC857FB365C56E8392737462A12", PublicKeyUrl = "http://utox.org/qkey" }
        };

        public ToxNameService[] NameServices
        {
            get { return nameServices; }
            set { nameServices = value; }
        }
    }

    [Serializable]
    public class ToxConfigNode
    {
        public string ClientId { get; set; }

        public string Address { get; set; }
        public int Port { get; set; }
    }

    [Serializable]
    public class ToxNameService
    {
        public string Domain { get; set; }
        public string PublicKey { get; set; }
        public string PublicKeyUrl { get; set; }
    }
}
