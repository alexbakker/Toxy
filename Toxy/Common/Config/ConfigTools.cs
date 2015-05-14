using System.IO;
using System.Xml.Serialization;

namespace Toxy.Common
{
    static class ConfigTools
    {
        public static void Save(Config config, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                serializer.Serialize(stream, config);
            }
        }

        public static Config Load(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                return (Config)serializer.Deserialize(stream);
            }
        }
    }
}
