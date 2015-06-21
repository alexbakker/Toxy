using System;
using System.Xml.Serialization;
using System.IO;
using Toxy.ViewModels;

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

        public string ProfilePath { get; set; }

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
}
