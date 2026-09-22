using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KanbanNW.Views;

/// <summary>
/// Simple confirmation dialog with a message and Confirm/Cancel buttons.
/// </summary>
public partial class ConfirmDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmDialog"/> class.
    /// </summary>
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows a confirmation dialog and returns true if the user confirmed.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    /// <param name="message">The message to display to the user.</param>
    /// <returns>True if user clicked Confirm, false if Cancel or closed.</returns>
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