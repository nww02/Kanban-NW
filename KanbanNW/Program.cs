using Avalonia;
using System;

namespace KanbanNW;

/// <summary>
/// Application entry point. Configures and starts the Avalonia application.
/// </summary>
sealed class Program
{
    /// <summary>
    /// Main entry point. STAThread is required for Avalonia on Windows.
    /// </summary>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Builds and configures the Avalonia application.
    /// Also used by the visual designer at design-time.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()             // Auto-detect OS and load appropriate backend (Win32, X11, etc.)
#if DEBUG
            .WithDeveloperTools()           // Enable developer tools (F12) in debug builds
#endif
            .LogToTrace();                  // Log Avalonia internal messages to trace output
}