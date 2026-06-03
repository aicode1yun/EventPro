using Ticket.Helpers;

namespace Ticket
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Register global exception handlers early so we capture unhandled exceptions
            // from managed code and surface more diagnostic information in logs.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"UnhandledDomainException: {e.ExceptionObject}");
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"UnobservedTaskException: {e.Exception}");
                    e.SetObserved();
                }
                catch { }
            };

            // Note: capturing Android JNI unhandled exceptions via managed API is limited.
            // We avoid calling nonexistent APIs here. Use logcat and the global handlers above
            // to capture exception details. If needed, add a Java.Lang.Thread.DefaultUncaughtExceptionHandler
            // implementation in Platforms/Android to log Java exceptions.
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Defer reading platform preferences until window creation when platform services
            // (Preferences, SecureStorage, etc.) are initialized. Wrap in try/catch so any
            // platform-specific error is captured without crashing the app at startup.
            try
            {
                var theme = SecureStorageHelper.GetThemeMode();
                UserAppTheme = theme == "dark" ? AppTheme.Dark : AppTheme.Light;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read stored theme: {ex}");
            }

            return new Window(new AppShell());
        }
    }
}
