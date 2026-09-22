using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KanbanNW.Data;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

/// <summary>
/// Dialog for managing columns (Config -> Columns).
/// Supports add/rename/delete with Save/Cancel pattern.
/// </summary>
public partial class ColumnsEditorWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnsEditorWindow"/> class.
    /// </summary>
    public ColumnsEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the Columns Editor dialog.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    /// <param name="projectId">The ID of the project whose columns to edit.</param>
    public static async Task ShowAsync(Window owner, int projectId)
    {
        var db = new KanbanDbContext();
        var vm = new ColumnsEditorViewModel(db, projectId);
        vm.ShowConfirmCallback = async msg => await ConfirmDialog.ShowAsync(owner, msg);
        var dialog = new ColumnsEditorWindow
        {
            DataContext = vm
        };
        await dialog.ShowDialog(owner);
        db.Dispose();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ColumnsEditorViewModel vm)
        {
            vm.Save();
        }
        Close();
    }
}