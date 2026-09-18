using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KanbanNW.Views;

public partial class AddColumnDialog : Window
{
    public AddColumnDialog()
    {
        InitializeComponent();
    }

    public string ColumnName => ColumnNameInput.Text?.Trim() ?? string.Empty;

    public static async Task<string?> ShowAsync(Window owner)
    {
        var dialog = new AddColumnDialog();
        await dialog.ShowDialog(owner);
        return dialog.DialogResult;
    }

    private string? DialogResult { get; set; }

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        var name = ColumnNameInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;
        DialogResult = name;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        DialogResult = null;
        Close();
    }
}