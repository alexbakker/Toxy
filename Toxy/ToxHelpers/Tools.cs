using SharpTox.Core;

namespace Toxy.ToxHelpers
{
    static class Tools
    {
        public static string GetAFError(ToxAFError error)
        {
            switch(error)
            {
                case ToxAFError.AlreadySent:
                    return "This person is already in your friend list.";
                case ToxAFError.SetNewNospam:
                    return "This person is already in your friend list but the nospam value of this id is different. (The nospam value for that friend was set to this new one)";
                case ToxAFError.NoMessage:
                    return "You can't send a friend request with an empty message.";
                case ToxAFError.OwnKey:
                    return "You can't add yourself to your friend list.";
                case ToxAFError.NoMem:
                    return "Something went wrong while increasing the size of your friend list.";
                case ToxAFError.BadChecksum:
                    return "The checksum in this address is bad.";
                default:
                    return "An unknown error occurred";
            }
        }

        public static string GetSizeString(long byteCount)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double length = byteCount;
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
