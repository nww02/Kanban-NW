using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KanbanNW.Converters;
using KanbanNW.Data;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

/// <summary>
/// Dialog for editing task category names (Config -> Categories).
/// </summary>
public partial class CategoryEditorWindow : Window
{
    private readonly KanbanDbContext? _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryEditorWindow"/> class.
    /// </summary>
    public CategoryEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryEditorWindow"/> class with a database context.
    /// </summary>
    /// <param name="db">The database context to use.</param>
    public CategoryEditorWindow(KanbanDbContext db) : this()
    {
        _db = db;
    }

    /// <summary>
    /// Shows the Category Editor dialog.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    /// <param name="db">The database context to use.</param>
    public static async Task ShowAsync(Window owner, KanbanDbContext db)
    {
        var vm = new CategoryEditorViewModel(db);
        var dialog = new CategoryEditorWindow(db)
        {
            DataContext = vm
        };
        await dialog.ShowDialog(owner);

        // Reload the name cache so the board updates immediately
        TaskTypeNameCache.Reload(db);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CategoryEditorViewModel vm)
        {
            vm.Save();
        }
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}