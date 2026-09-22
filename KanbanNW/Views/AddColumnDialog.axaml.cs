using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KanbanNW.Views;

/// <summary>
/// Simple dialog for entering a new column name.
/// </summary>
public partial class AddColumnDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddColumnDialog"/> class.
    /// </summary>
    public AddColumnDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The entered column name (trimmed), or empty string if cancelled.
    /// </summary>
    public string ColumnName => ColumnNameInput.Text?.Trim() ?? string.Empty;

    /// <summary>
    /// Shows the dialog and returns the entered column name, or null if cancelled.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    /// <returns>The entered column name, or null if cancelled.</returns>
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