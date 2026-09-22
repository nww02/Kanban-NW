using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using KanbanNW.Converters;
using KanbanNW.Data;
using KanbanNW.ViewModels;
using KanbanNW.Views;

namespace KanbanNW;

/// <summary>
/// Application class. Handles startup, theme management, and resource initialization.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the application by loading XAML resources.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Called when the framework has completed initialization.
    /// Sets up the database, loads user settings, applies theme, and creates the main window.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var db = new KanbanDbContext();

            // Load custom TaskType display names from database into static cache
            TaskTypeNameCache.Load(db);

            // Load font size settings from database and apply to application resources
            var fontSettings = Views.AppearanceSettingsWindow.LoadFromDb(db);
            Views.AppearanceSettingsWindow.ApplyToResources(fontSettings);

            // Load and apply saved theme variant (Light/Dark/System)
            var themeStr = db.GetSetting("Theme") ?? "Light";
            var theme = themeStr.ToLower() switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
            Application.Current!.RequestedThemeVariant = theme;
            ApplyThemeResources(theme);

            // Subscribe to OS theme changes when in System/Default mode
            Application.Current!.ActualThemeVariantChanged += (_, _) =>
                ApplyThemeResources(Application.Current!.RequestedThemeVariant);

            // Create main ViewModel and main window
            var mainVm = new MainViewModel(db);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Updates all custom color brushes in Application.Resources to match the requested theme.
    /// When requestedTheme is Default (System mode), resolves the actual theme from ActualThemeVariant.
    /// Call this whenever RequestedThemeVariant changes or the OS theme changes.
    /// </summary>
    /// <param name="requestedTheme">The theme variant requested by the user (Light, Dark, or Default/System).</param>
    public static void ApplyThemeResources(ThemeVariant requestedTheme)
    {
        var res = Application.Current!.Resources;

        // Resolve actual theme: in System/Default mode, use the OS-reported ActualThemeVariant
        var theme = requestedTheme == ThemeVariant.Default
            ? Application.Current!.ActualThemeVariant
            : requestedTheme;
        var isDark = theme == ThemeVariant.Dark;

        // Replace each brush with the appropriate color for the current theme
        // This uses brush REPLACEMENT (not mutation) so DynamicResource bindings re-evaluate correctly
        ReplaceBrush(res, "WindowBackground", isDark ? "#1E1E1E" : "#FAFAFA");
        ReplaceBrush(res, "MenuBarBackground", isDark ? "#1A1A1A" : "#E8E8E8");
        ReplaceBrush(res, "TabBarColor", isDark ? "#252525" : "#D5D5D5");
        ReplaceBrush(res, "MenuForeground", isDark ? "#E0E0E0" : "#222222");
        ReplaceBrush(res, "TabTextForeground", isDark ? "#E0E0E0" : "#222222");
        ReplaceBrush(res, "ColumnBackground", isDark ? "#2D2D2D" : "#F0F2F5");
        ReplaceBrush(res, "TaskBackground", isDark ? "#383838" : "White");
        ReplaceBrush(res, "ColumnHeaderBgCol", isDark ? "#2D2D2D" : "#E3F2FD");
        ReplaceBrush(res, "ColumnHeaderForeground", isDark ? "#E0E0E0" : "Black");
        ReplaceBrush(res, "TaskTitleForeground", isDark ? "#E0E0E0" : "Black");
        ReplaceBrush(res, "BorderColor", isDark ? "#555555" : "#E0E0E0");
        ReplaceBrush(res, "SubtleBorderColor", isDark ? "#404040" : "#F0F0F0");
        ReplaceBrush(res, "HintForeground", isDark ? "#999999" : "#888888");
    }

    /// <summary>
    /// Helper to replace a SolidColorBrush in the resource dictionary with a new instance.
    /// This ensures DynamicResource bindings are notified of the change.
    /// </summary>
    /// <param name="res">The resource dictionary.</param>
    /// <param name="key">The resource key to replace.</param>
    /// <param name="colorStr">The hex color string for the new brush.</param>
    private static void ReplaceBrush(IDictionary<object, object?> res, string key, string colorStr)
    {
        res[key] = new SolidColorBrush(Color.Parse(colorStr));
    }
}