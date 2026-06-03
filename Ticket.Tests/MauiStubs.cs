// Stubs for MAUI types used by the source files under test

namespace Microsoft.Maui.Storage
{
    public static class FileSystem
    {
        public static string AppDataDirectory => Path.GetTempPath();
    }
}

namespace Microsoft.Maui.Controls
{
    public static class Shell
    {
        public static object? Current { get; set; }
    }

    public enum AppTheme
    {
        Light,
        Dark,
        Unspecified
    }

    public class Application
    {
        public static Application? Current { get; set; }
        public AppTheme UserAppTheme { get; set; }
    }
}
