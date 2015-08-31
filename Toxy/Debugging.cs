using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
namespace Toxy
{
    public static class Debugging
    {
        [Conditional("DEBUG")]
        public static void Write(string text, [CallerFilePath] string filename = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            Debug.WriteLine("{0}_{1}({2}): {3}", Path.GetFileName(filename), member, line, text);
        }
    }
}
