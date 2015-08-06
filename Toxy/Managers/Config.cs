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
}
