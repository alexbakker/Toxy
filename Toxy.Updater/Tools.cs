using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Toxy.Updater
{
    static class Tools
    {
        public static bool IsX64
        {
            get
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
                    return true;
                else
                    return false;
            }
        }

        public static bool VerifyCertificate(byte[] certData, string publicKey, out string message)
        {
            var chain = new X509Chain();

            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;

            var cert = new X509Certificate2(certData);
            bool success = chain.Build(cert);

            if (chain.ChainStatus.Count() > 0)
                message = string.Format("{0}\n{1}", chain.ChainStatus[0].Status, chain.ChainStatus[0].StatusInformation);
            else
                message = string.Empty;

            if (!success)
                return false;

            if (cert.GetPublicKeyString() != publicKey)
            {
                message = "Public keys don't match";
                return false;
            }

            return true;
        }
    }
}
