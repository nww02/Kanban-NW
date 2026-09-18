using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using KanbanNW.Data;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

public partial class ProjectEditorWindow : Window
{
    private static readonly string[] ColorOptions = { "#2C3E50", "#34495E", "#1A5276", "#922B21", "#1E8449", "#B7950B", "#6C3483", "#2E86C1", "#616A6B", "#D35400", "#27AE60" };

    public ProjectEditorWindow()
    {
        InitializeComponent();
    }

    public static async Task ShowAsync(Window owner, KanbanDbContext db)
    {
        var vm = new ProjectEditorViewModel(db);
        var dialog = new ProjectEditorWindow
        {
            DataContext = vm
        };
        await dialog.ShowDialog(owner);
    }

    private async void OnColorSwatchClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ProjectItem item)
            return;

        var currentColor = Color.TryParse(item.Color, out var parsed) ? parsed : Color.Parse("#2C3E50");
        var picked = await ColorPickerDialog.PickAsync(this, currentColor);
        if (picked.HasValue)
        {
            item.Color = $"#{picked.Value.R:X2}{picked.Value.G:X2}{picked.Value.B:X2}";
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProjectEditorViewModel vm)
        {
            vm.SaveChanges();
        }
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}