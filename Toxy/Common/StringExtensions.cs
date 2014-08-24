using System;
using System.Text;
using System.Collections.Generic;

namespace Toxy.Common
{
    public static class StringExtensions
    {
        public static List<string> WordWrap(this string s, int length)
        {
            if (s.Length == 0)
                return new List<string>();

            List<string> lines = new List<string>();
            string current = "";

            foreach (var part in s.Split(' '))
            {
                if ((current.GetByteCount() > length) || ((current.GetByteCount() + part.GetByteCount()) > length))
                {
                    lines.Add(current);
                    current = "";
                }

                if (current.GetByteCount() > 0)
                    current += " " + part;
                else
                    current += part;
            }

            if (current.GetByteCount() > 0)
                lines.Add(current);

            return lines;
        }

        private static int GetByteCount(this string s)
        {
            return Encoding.UTF8.GetByteCount(s);
        }
    }
}
