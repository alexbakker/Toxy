using System;
using System.Xml.Serialization;
using System.IO;
using Toxy.ViewModels;
using SharpTox.Core;

namespace Toxy.Managers
{
    [Serializable]
    public class Config
    {
        public static string ConfigPath = Path.Combine(ProfileManager.ProfileDataPath, "Toxy");
        private const string _fileName = "config.xml";
        private static Config _instance;

        [XmlIgnore]
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Config();

                return _instance;
            }
        }

        private Config() { }

        public DeviceInfo RecordingDevice { get; set; }
        public DeviceInfo PlaybackDevice { get; set; }
        public DeviceInfo VideoDevice { get; set; }
        public bool SendTypingNotifications { get; set; }
        public string ProfilePath { get; set; }
        public int AwayTimeMinutes { get; set; } = 1;
        public bool EnableAutoAway { get; set; }
        public string ProxyAddress { get; set; }
        public int ProxyPort { get; set; }
        public ToxProxyType ProxyType { get; set; }

        public ToxNameService[] NameServices { get; set; } = new[]
        {
            new ToxNameService { Domain = "toxme.se", PublicKey = "5D72C517DF6AEC54F1E977A6B6F25914EA4CF7277A85027CD9F5196DF17E0B13" },
            new ToxNameService { Domain = "utox.org", PublicKey = "D3154F65D28A5B41A05D4AC7E4B39C6B1C233CC857FB365C56E8392737462A12" }
        };

        public ToxConfigNode[] Nodes { get; set; } = new[]
        {
            new ToxConfigNode { PublicKey = "951C88B7E75C867418ACDB5D273821372BB5BD652740BCDF623A4FA293E75D2F", Address = "192.254.75.98", Port = 33445 },
            new ToxConfigNode { PublicKey = "788236D34978D1D5BD822F0A5BEBD2C53C64CC31CD3149350EE27D4D9A2F9B6B", Address = "178.62.250.138", Port = 33445 },
            new ToxConfigNode { PublicKey = "04119E835DF3E78BACF0F84235B300546AF8B936F035185E2A8E9E0A67C8924F", Address = "144.76.60.215", Port = 33445 },
            new ToxConfigNode { PublicKey = "A09162D68618E742FFBCA1C2C70385E6679604B2D80EA6E84AD0996A1AC8A074", Address = "23.226.230.47", Port = 33445 },
            new ToxConfigNode { PublicKey = "10B20C49ACBD968D7C80F2E8438F92EA51F189F4E70CFBBB2C2C8C799E97F03E", Address = "178.62.125.224", Port = 33445 },
            new ToxConfigNode { PublicKey = "5EB67C51D3FF5A9D528D242B669036ED2A30F8A60E674C45E7D43010CB2E1331", Address = "37.187.46.132", Port = 33445 },
            new ToxConfigNode { PublicKey = "4B2C19E924972CB9B57732FB172F8A8604DE13EEDA2A6234E348983344B23057", Address = "178.21.112.187", Port = 33445 },
            new ToxConfigNode { PublicKey = "E398A69646B8CEACA9F0B84F553726C1C49270558C57DF5F3C368F05A7D71354", Address = "195.154.119.113", Port = 33445 },
            new ToxConfigNode { PublicKey = "F404ABAA1C99A9D37D61AB54898F56793E1DEF8BD46B1038B9D822E8460FAB67", Address = "192.210.149.121", Port = 33445 },
            new ToxConfigNode { PublicKey = "7F9C31FE850E97CEFD4C4591DF93FC757C7C12549DDD55F8EEAECC34FE76C029", Address = "54.199.139.199", Port = 33445 },
            new ToxConfigNode { PublicKey = "93574A3FAB7D612FEA29FD8D67D3DD10DFD07A075A5D62E8AF3DD9F5D0932E11", Address = "76.191.23.96", Port = 33445 },
            new ToxConfigNode { PublicKey = "F5A1A38EFB6BD3C2C8AF8B10D85F0F89E931704D349F1D0720C3C4059AF2440A", Address = "46.38.239.179", Port = 33445 },
            new ToxConfigNode { PublicKey = "2C308B4518862740AD9A121598BCA7713AFB25858B747313A4D073E2F6AC506C", Address = "144.76.93.230", Port = 33445 }
        };

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigPath);

                using (FileStream stream = new FileStream(Path.Combine(ConfigPath, _fileName), FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Config));
                    serializer.Serialize(stream, this);
                }

                Debugging.Write("Saved config to disk");
            }
            catch (Exception ex) { Debugging.Write("Could not save config: " + ex.ToString()); }
        }

        public void Reload()
        {
            try
            {
                using (FileStream stream = new FileStream(Path.Combine(ConfigPath, _fileName), FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Config));
                    _instance = (Config)serializer.Deserialize(stream);
                }

                Debugging.Write("Reloaded config from disk");
            }
            catch (Exception ex) { Debugging.Write("Could not reload config: " + ex.ToString()); }
        }
    }

    [Serializable]
    public class ToxNameService
    {
        public string Domain { get; set; }
        public string PublicKey { get; set; }
    }

    [Serializable]
    public class ToxConfigNode
    {
        public string PublicKey { get; set; }

        public string Address { get; set; }
        public int Port { get; set; }
    }
}
