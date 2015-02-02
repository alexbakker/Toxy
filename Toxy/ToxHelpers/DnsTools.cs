using System;
using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Linq;
using System.Runtime.InteropServices;

using Toxy.Common;
using SharpTox.Dns;


namespace Toxy.ToxHelpers
{
    static class DnsTools
    {
        [DllImport("dnsapi", EntryPoint = "DnsQuery_W", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern int DnsQuery([MarshalAs(UnmanagedType.VBByRefStr)]ref string pszName, QueryTypes wType, QueryOptions options, int aipServers, ref IntPtr ppQueryResults, int pReserved);

        [DllImport("dnsapi", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void DnsRecordListFree(IntPtr pRecordList, int FreeType);

        private static ToxNameService FindNameServiceFromStore(ToxNameService[] services, string suffix)
        {
            return services.Where(s => s.Domain == suffix).First();
        }

        public static ToxNameService FindNameService(string domain)
        {
            for (int i = 0; i < 3; i++)
            {
                string[] records = GetSPFRecords("_tox." + domain);

                foreach (string record in records)
                {
                    if (!string.IsNullOrEmpty(record))
                        return new ToxNameService() { Domain = domain, PublicKey = record };
                }
            }

            return null;
        }

        public static string DiscoverToxID(string domain, ToxNameService[] services, bool localStoreOnly)
        {
            ToxNameService service;

            if (!localStoreOnly)
                service = FindNameService(domain.Split('@')[1]) ?? FindNameServiceFromStore(services, domain.Split('@')[1]);
            else
                service = FindNameServiceFromStore(services, domain.Split('@')[1]);

            if (service == null)
            {
                //this name service does not use tox3, how unencrypted of them
                domain = domain.Replace("@", "._tox.");

                string[] records = GetSPFRecords(domain);

                foreach (string record in records)
                {
                    if (record.Contains("v=tox1"))
                    {
                        string[] entries = record.Split(';');

                        foreach (string entry in entries)
                        {
                            string[] parts = entry.Split('=');
                            string name = parts[0];
                            string value = parts[1];

                            if (name == "id")
                                return value;
                        }
                    }
                }
            }
            else
            {
                string public_key;

                if (!string.IsNullOrWhiteSpace(service.PublicKey))
                    public_key = service.PublicKey;
                else
                    return null;

                string[] split = domain.Split('@');

                ToxDns tox_dns = new ToxDns(public_key);
                uint request_id;
                string dns3_string = tox_dns.GenerateDns3String(split[0], out request_id);

                string query = string.Format("_{0}._tox.{1}", dns3_string, split[1]);

                string[] records = GetSPFRecords(query);

                foreach (string record in records)
                {
                    if (record.Contains("v=tox3"))
                    {
                        string[] entries = record.Split(';');

                        foreach (string entry in entries)
                        {
                            string[] parts = entry.Split('=');
                            string name = parts[0];
                            string value = parts[1];

                            if (name == "id")
                            {
                                string result = tox_dns.DecryptDns3TXT(value, request_id);

                                tox_dns.Dispose();
                                return result;
                            }
                        }
                    }
                }

                tox_dns.Dispose();
            }

            return null;
        }

        private static string[] GetSPFRecords(string domain)
        {
            IntPtr ptr1 = IntPtr.Zero;
            IntPtr ptr2 = IntPtr.Zero;

            SPFRecord recSPF;

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                throw new NotSupportedException();

            ArrayList list = new ArrayList();
            try
            {

                int num1 = DnsQuery(ref domain, QueryTypes.DNS_TYPE_TXT, QueryOptions.DNS_QUERY_BYPASS_CACHE, 0, ref ptr1, 0);
                if (num1 != 0)
                    throw new Win32Exception(num1);

                for (ptr2 = ptr1; !ptr2.Equals(IntPtr.Zero); ptr2 = recSPF.pNext)
                {
                    recSPF = (SPFRecord)Marshal.PtrToStructure(ptr2, typeof(SPFRecord));
                    if (recSPF.wType == (short)QueryTypes.DNS_TYPE_TXT)
                    {
                        for (int i = 0; i < recSPF.dwStringCount; i++)
                        {
                            IntPtr pString = recSPF.pStringArray + i;
                            string s = Marshal.PtrToStringAuto(pString);

                            list.Add(s);
                        }
                    }
                }
            }
            finally
            {
                DnsRecordListFree(ptr1, 0);
            }

            return (string[])list.ToArray(typeof(string));
        }

        private enum QueryOptions
        {
            DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 1,
            DNS_QUERY_BYPASS_CACHE = 8,
            DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
            DNS_QUERY_NO_HOSTS_FILE = 0x40,
            DNS_QUERY_NO_LOCAL_NAME = 0x20,
            DNS_QUERY_NO_NETBT = 0x80,
            DNS_QUERY_NO_RECURSION = 4,
            DNS_QUERY_NO_WIRE_QUERY = 0x10,
            DNS_QUERY_RESERVED = -16777216,
            DNS_QUERY_RETURN_MESSAGE = 0x200,
            DNS_QUERY_STANDARD = 0,
            DNS_QUERY_TREAT_AS_FQDN = 0x1000,
            DNS_QUERY_USE_TCP_ONLY = 2,
            DNS_QUERY_WIRE_ONLY = 0x100
        }

        private enum QueryTypes
        {
            DNS_TYPE_A = 1,
            DNS_TYPE_NS = 2,
            DNS_TYPE_CNAME = 5,
            DNS_TYPE_SOA = 6,
            DNS_TYPE_PTR = 12,
            DNS_TYPE_HINFO = 13,
            DNS_TYPE_MX = 15,
            DNS_TYPE_TXT = 16,
            DNS_TYPE_AAAA = 28
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SPFRecord
        {
            public IntPtr pNext;
            public string pName;
            public short wType;
            public short wDataLength;
            public int flags;
            public int dwTtl;
            public int dwReserved;
            public Int32 dwStringCount;
            public IntPtr pStringArray;
        }
    }
}
