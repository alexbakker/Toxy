using System;

namespace Toxy.Extensions
{
    public static class LongExtensions
    {
        public static string GetSizeString(this long l)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double length = l;
            int i;

            for (i = 0; i < sizes.Length; i++)
            {
                if (length < 1024)
                    break;

                length = length / 1024;
            }

            return string.Format("{0:0.##}{1}", length, sizes[i]);
        }
    }
}
