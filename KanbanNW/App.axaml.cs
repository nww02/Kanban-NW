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

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var db = new KanbanDbContext();

            // Load custom TaskType display names
            TaskTypeNameCache.Load(db);

            // Load font size settings from DB
            var fontSettings = Views.AppearanceSettingsWindow.LoadFromDb(db);
            Views.AppearanceSettingsWindow.ApplyToResources(fontSettings);

            // Apply theme from saved settings
            var themeStr = db.GetSetting("Theme") ?? "Light";
            var theme = themeStr.ToLower() switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
            Application.Current!.RequestedThemeVariant = theme;
            ApplyThemeResources(theme);

            // React to system theme changes when in "Default" (System) mode
            Application.Current!.ActualThemeVariantChanged += (_, _) =>
                ApplyThemeResources(Application.Current!.RequestedThemeVariant);

            var mainVm = new MainViewModel(db);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Updates custom color brushes in Application.Resources to match the given theme variant.
    /// When requestedTheme is Default (System mode), resolves the actual theme variant.
    /// Call this whenever RequestedThemeVariant is changed or the OS theme changes.
    /// </summary>
    public static void ApplyThemeResources(ThemeVariant requestedTheme)
    {
        var res = Application.Current!.Resources;

        // Resolve actual theme: when in System/Default mode, use ActualThemeVariant
        var theme = requestedTheme == ThemeVariant.Default
            ? Application.Current!.ActualThemeVariant
            : requestedTheme;
        var isDark = theme == ThemeVariant.Dark;

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

    private static void ReplaceBrush(IDictionary<object, object?> res, string key, string colorStr)
    {
        res[key] = new SolidColorBrush(Color.Parse(colorStr));
    }
}