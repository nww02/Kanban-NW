using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KanbanNW.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows a confirmation dialog and returns true if the user confirmed.
    /// </summary>
    public static async Task<bool> ShowAsync(Window owner, string message)
    {
        var dialog = new ConfirmDialog();
        dialog.MessageText.Text = message;
        return await dialog.ShowDialog<bool>(owner);
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}