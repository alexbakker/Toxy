namespace Toxy
{
    public static class BuildInfo
    {
#if DEBUG
        public static string TitleFormat { get; } = "Toxy {0} DEBUG - {1}";
#else
        public static string TitleFormat { get; } = "Toxy - {1}";
#endif
        public static string CommitHash { get; } = "";
        public static string BuildNumber { get; } = "";
        public static string Platform { get; } = "";
        public static string Date { get; } = "";
    }
}
