using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace KanbanNW.Views;

public partial class AppearanceSettingsWindow : Window
{
    private static readonly Dictionary<string, object> DefaultValues = new()
    {
        ["TabFontSize"] = 18.0, ["ColumnHeaderFontSize"] = 18.0, ["TaskTitleFontSize"] = 16.0,
        ["TaskDescFontSize"] = 14.0, ["TaskButtonFontSize"] = 14.0, ["TypeBadgeFontSize"] = 14.0,
        ["DueDateFontSize"] = 14.0, ["AddTaskBtnFontSize"] = 14.0, ["EmptyQueueBtnFontSize"] = 14.0,
        ["TaskEditorFontSize"] = 14.0, ["BaseFontSize"] = 14.0,
        ["AppFontFamily"] = "Inter",
        ["Theme"] = "Light", ["FontPreset"] = "Medium"
    };

    public AppearanceSettingsWindow()
    {
        InitializeComponent();
    }

    public static Dictionary<string, object> LoadFromDb(Data.KanbanDbContext db)
    {
        var values = new Dictionary<string, object>();
        foreach (var key in DefaultValues.Keys)
        {
            var val = db.GetSetting(key);
            if (val == null)
            {
                values[key] = DefaultValues[key];
                continue;
            }
            if (key.EndsWith("FontSize") || key == "BaseFontSize")
            {
                if (double.TryParse(val, out var d))
                    values[key] = d;
                else
                    values[key] = DefaultValues[key];
            }
            else
            {
                values[key] = val;
            }
        }
        return values;
    }

    public static void ApplyToResources(Dictionary<string, object> values)
    {
        foreach (var (key, val) in values)
        {
            if (key.EndsWith("FontSize") || key == "BaseFontSize")
                Application.Current!.Resources[key] = val is double d ? d : double.Parse(val.ToString()!);
            else if (key == "AppFontFamily")
                Application.Current!.Resources[key] = new FontFamily(val.ToString()!);
        }
    }

    public static void SaveToDb(Data.KanbanDbContext db, Dictionary<string, object> values)
    {
        foreach (var (key, val) in values)
        {
            var strVal = val is double d ? d.ToString("F0") : val.ToString()!;
            db.SaveSetting(key, strVal);
        }
        ApplyToResources(values);

        // Apply theme variant immediately and update custom brushes
        var themeStr = values.TryGetValue("Theme", out var t) ? t.ToString() ?? "Light" : "Light";
        var theme = themeStr.ToLower() switch
        {
            "dark" => ThemeVariant.Dark,
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
        Application.Current!.RequestedThemeVariant = theme;
        App.ApplyThemeResources(theme);
    }

    public static async System.Threading.Tasks.Task ShowAsync(Window owner)
    {
        var db = new Data.KanbanDbContext();
        var currentValues = LoadFromDb(db);
        var vm = new ViewModels.AppearanceSettingsViewModel(currentValues);
        var dialog = new AppearanceSettingsWindow { DataContext = vm };
        await dialog.ShowDialog(owner);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.AppearanceSettingsViewModel vm)
        {
            var values = vm.GetValues();
            SaveToDb(new Data.KanbanDbContext(), values);
        }
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnReset(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.AppearanceSettingsViewModel vm)
        {
            vm.ResetToDefaults();
        }
    }
}