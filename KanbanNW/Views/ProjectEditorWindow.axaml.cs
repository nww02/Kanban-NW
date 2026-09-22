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

/// <summary>
/// Dialog for managing projects (Config -> Projects).
/// Supports add/rename/delete projects and changing their colors.
/// </summary>
public partial class ProjectEditorWindow : Window
{
    private static readonly string[] ColorOptions = new[]
    {
        "#2C3E50", "#34495E", "#1A5276", "#922B21", "#1E8449",
        "#B7950B", "#6C3483", "#2E86C1", "#616A6B", "#D35400", "#27AE60"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectEditorWindow"/> class.
    /// </summary>
    public ProjectEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the Project Editor dialog.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    /// <param name="db">The database context to use.</param>
    public static async Task ShowAsync(Window owner, KanbanDbContext db)
    {
        var vm = new ProjectEditorViewModel(db);
        var dialog = new ProjectEditorWindow
        {
            DataContext = vm
        };
        await dialog.ShowDialog(owner);
    }

    /// <summary>
    /// Handles clicks on the color swatch to open the color picker.
    /// </summary>
    /// <param name="sender">The border that was clicked.</param>
    /// <param name="e">The pointer event arguments.</param>
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