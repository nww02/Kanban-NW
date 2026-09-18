using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KanbanNW.ViewModels;

public partial class AppearanceSettingsViewModel : ViewModelBase
{
    // --- Font size properties ---
    [ObservableProperty] private int _tabFontSize;
    [ObservableProperty] private int _columnHeaderFontSize;
    [ObservableProperty] private int _taskTitleFontSize;
    [ObservableProperty] private int _taskDescFontSize;
    [ObservableProperty] private int _taskButtonFontSize;
    [ObservableProperty] private int _typeBadgeFontSize;
    [ObservableProperty] private int _dueDateFontSize;
    [ObservableProperty] private int _addTaskBtnFontSize;
    [ObservableProperty] private int _emptyQueueBtnFontSize;
    [ObservableProperty] private int _taskEditorFontSize;
    [ObservableProperty] private int _baseFontSize;

    // --- Font family ---
    [ObservableProperty] private string _selectedFont = "Inter";

    // --- Theme ---
    [ObservableProperty] private string _selectedTheme = "Light";
    [ObservableProperty] private string _selectedFontPreset = "Medium";

    public string[] Themes { get; } = { "System", "Light", "Dark" };
    public string[] FontPresets { get; } = { "Small", "Medium", "Large" };

    public string[] Fonts { get; } = GetSystemFonts();

    partial void OnSelectedThemeChanged(string value)
    {
        ApplySettings();
    }

    private static readonly Dictionary<string, int[]> FontPresetValues = new()
    {
        ["Small"]  = new[] { 14, 14, 12, 11, 11, 11, 11, 11, 11, 11, 11 },
        ["Medium"] = new[] { 18, 18, 16, 14, 14, 14, 14, 14, 14, 14, 14 },
        ["Large"]  = new[] { 22, 22, 20, 18, 18, 18, 18, 18, 18, 18, 18 }
    };

    public AppearanceSettingsViewModel(Dictionary<string, object> currentValues)
    {
        TabFontSize             = GetInt(currentValues, "TabFontSize", 18);
        ColumnHeaderFontSize    = GetInt(currentValues, "ColumnHeaderFontSize", 18);
        TaskTitleFontSize       = GetInt(currentValues, "TaskTitleFontSize", 16);
        TaskDescFontSize        = GetInt(currentValues, "TaskDescFontSize", 14);
        TaskButtonFontSize      = GetInt(currentValues, "TaskButtonFontSize", 14);
        TypeBadgeFontSize       = GetInt(currentValues, "TypeBadgeFontSize", 14);
        DueDateFontSize         = GetInt(currentValues, "DueDateFontSize", 14);
        AddTaskBtnFontSize      = GetInt(currentValues, "AddTaskBtnFontSize", 14);
        EmptyQueueBtnFontSize   = GetInt(currentValues, "EmptyQueueBtnFontSize", 14);
        TaskEditorFontSize      = GetInt(currentValues, "TaskEditorFontSize", 14);
        BaseFontSize            = GetInt(currentValues, "BaseFontSize", 14);

        SelectedFont = currentValues.TryGetValue("AppFontFamily", out var af) ? af.ToString() ?? "Inter" : "Inter";
        SelectedTheme = currentValues.TryGetValue("Theme", out var th) ? th.ToString() ?? "Light" : "Light";
        SelectedFontPreset = currentValues.TryGetValue("FontPreset", out var fp) ? fp.ToString() ?? "Medium" : "Medium";
    }

    public void ApplySettings()
    {
        var theme = SelectedTheme?.ToLower() switch
        {
            "dark" => Avalonia.Styling.ThemeVariant.Dark,
            "light" => Avalonia.Styling.ThemeVariant.Light,
            _ => Avalonia.Styling.ThemeVariant.Default
        };
        Avalonia.Application.Current!.RequestedThemeVariant = theme;
        App.ApplyThemeResources(theme);
    }

    partial void OnSelectedFontPresetChanged(string value)
    {
        if (FontPresetValues.TryGetValue(value, out var sizes))
        {
            TabFontSize = sizes[0];
            ColumnHeaderFontSize = sizes[1];
            TaskTitleFontSize = sizes[2];
            TaskDescFontSize = sizes[3];
            TaskButtonFontSize = sizes[4];
            TypeBadgeFontSize = sizes[5];
            DueDateFontSize = sizes[6];
            AddTaskBtnFontSize = sizes[7];
            EmptyQueueBtnFontSize = sizes[8];
            TaskEditorFontSize = sizes[9];
            BaseFontSize = sizes[10];
        }
    }

    private static string[] GetSystemFonts()
    {
        var fonts = new List<string> { "Inter", "Arial" };
        try
        {
            var systemFonts = FontManager.Current.SystemFonts
                .Select(f => f.Name).Distinct()
                .OrderBy(n => n);
            fonts.AddRange(systemFonts.Where(f => !fonts.Contains(f)));
        }
        catch
        {
            fonts.AddRange(new[] { "Segoe UI", "Verdana", "Tahoma", "Times New Roman", "Courier New" });
        }
        return fonts.ToArray();
    }

    private static int GetInt(Dictionary<string, object> dict, string key, int def)
    {
        if (dict.TryGetValue(key, out var val) && val is double d)
            return (int)Math.Round(d);
        return def;
    }

    public Dictionary<string, object> GetValues()
    {
        return new Dictionary<string, object>
        {
            ["TabFontSize"] = (double)TabFontSize,
            ["ColumnHeaderFontSize"] = (double)ColumnHeaderFontSize,
            ["TaskTitleFontSize"] = (double)TaskTitleFontSize,
            ["TaskDescFontSize"] = (double)TaskDescFontSize,
            ["TaskButtonFontSize"] = (double)TaskButtonFontSize,
            ["TypeBadgeFontSize"] = (double)TypeBadgeFontSize,
            ["DueDateFontSize"] = (double)DueDateFontSize,
            ["AddTaskBtnFontSize"] = (double)AddTaskBtnFontSize,
            ["EmptyQueueBtnFontSize"] = (double)EmptyQueueBtnFontSize,
            ["TaskEditorFontSize"] = (double)TaskEditorFontSize,
            ["BaseFontSize"] = (double)BaseFontSize,
            ["AppFontFamily"] = SelectedFont,
            ["Theme"] = SelectedTheme,
            ["FontPreset"] = SelectedFontPreset
        };
    }

    public void ResetToDefaults()
    {
        SelectedFontPreset = "Medium";
        SelectedFont = "Inter";
        SelectedTheme = "Light";
    }
}