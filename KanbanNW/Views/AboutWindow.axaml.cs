using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KanbanNW.Views;

/// <summary>
/// About dialog showing app name, version, and license disclaimer.
/// </summary>
public partial class AboutWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutWindow"/> class.
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the About dialog as a modal dialog.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    public static async Task ShowAsync(Window owner)
    {
        var dialog = new AboutWindow();
        await dialog.ShowDialog(owner);
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}