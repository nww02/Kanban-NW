using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KanbanNW.Converters;
using KanbanNW.Data;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

public partial class CategoryEditorWindow : Window
{
    private readonly KanbanDbContext _db = null!;

    public CategoryEditorWindow()
    {
        InitializeComponent();
    }

    public CategoryEditorWindow(KanbanDbContext db) : this()
    {
        _db = db;
    }

    public static async Task ShowAsync(Window owner, KanbanDbContext db)
    {
        var vm = new CategoryEditorViewModel(db);
        var dialog = new CategoryEditorWindow(db)
        {
            DataContext = vm
        };
        await dialog.ShowDialog(owner);

        // Reload the name cache so the board updates
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