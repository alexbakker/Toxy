using System.IO;
using System.Xml.Serialization;

namespace Toxy.Common
{
    static class ConfigTools
    {
        public static void Save(Config config, string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Create);
            XmlSerializer serializer = new XmlSerializer(typeof(Config));

            serializer.Serialize(stream, config);
            stream.Dispose();
        }

        public static Config Load(string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(Config));

            Config config = (Config)serializer.Deserialize(stream);
            stream.Dispose();

            return config;
        }
    }
}
