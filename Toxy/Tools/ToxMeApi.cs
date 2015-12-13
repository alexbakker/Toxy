using System.Net;
using System.Net.Security;
using SimpleJson;
using System;

namespace Toxy.Tools
{
    public class ToxMeApi
    {
        private string _uri
        {
            get { return string.Format("https://{0}/api", _domain); }
        }

        private string _domain;
        private IWebProxy _proxy;

        static ToxMeApi()
        {
            //check/set some ssl settings at startup
            if (ServicePointManager.EncryptionPolicy != EncryptionPolicy.RequireEncryption)
                throw new Exception("Forcing encryption is required");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
        }

        public ToxMeApi(string domain, IWebProxy proxy = null)
        {
            _domain = domain;
            _proxy = proxy;
        }

        public string LookupID(string name)
        {
            var obj = new JsonObject();
            obj["action"] = LookupAction.Name;
            obj["name"] = name;

            var res = PostRequest(obj);
            return res["tox_id"] as string;
        }

        private JsonObject PostRequest(JsonObject req)
        {
            using (var client = new WebClient())
            {
                client.Proxy = _proxy;

                string res = client.UploadString(_uri, req.ToString());
                return SimpleJson.SimpleJson.DeserializeObject<JsonObject>(res);
            }
        }

        /*private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            if (error == SslPolicyErrors.None)
                return true;

            return false;
        }*/

        private enum LookupAction
        {
            Name = 3,
            ID = 5,
            PartialName = 6
        }
    }
}
