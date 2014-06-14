using System;
using System.Net;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;

using SharpTox;

namespace Toxy
{
    static class DnsTools
    {
        [DllImport("dnsapi", EntryPoint = "DnsQuery_W", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern int DnsQuery([MarshalAs(UnmanagedType.VBByRefStr)]ref string pszName, QueryTypes wType, QueryOptions options, int aipServers, ref IntPtr ppQueryResults, int pReserved);

        [DllImport("dnsapi", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void DnsRecordListFree(IntPtr pRecordList, int FreeType);

        public static string DiscoverToxID(string domain)
        {
            if (domain.Contains("@utox.org"))
            {
                string[] split = domain.Split('@');
                string public_key = new WebClient().DownloadString("http://utox.org/qkey");

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

                                tox_dns.Kill();
                                return result;
                            }
                        }
                    }
                }

                tox_dns.Kill();
            }
            else if (domain.Contains("@"))
            {
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

                int num1 = DnsTools.DnsQuery(ref domain, QueryTypes.DNS_TYPE_TXT, QueryOptions.DNS_QUERY_BYPASS_CACHE, 0, ref ptr1, 0);
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
                DnsTools.DnsRecordListFree(ptr1, 0);
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
