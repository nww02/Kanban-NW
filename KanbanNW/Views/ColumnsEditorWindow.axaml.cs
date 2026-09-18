using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KanbanNW.Data;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

public partial class ColumnsEditorWindow : Window
{
    public ColumnsEditorWindow()
    {
        InitializeComponent();
    }

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