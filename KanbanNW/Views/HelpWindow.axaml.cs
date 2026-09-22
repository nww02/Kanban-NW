using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

/// <summary>
/// Help window showing documentation topics in a two-pane layout.
/// </summary>
public partial class HelpWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HelpWindow"/> class.
    /// </summary>
    public HelpWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the Help window as a modal dialog.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    public static async Task ShowAsync(Window owner)
    {
        var dialog = new HelpWindow
        {
            DataContext = new HelpViewModel()
        };
        await dialog.ShowDialog(owner);
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}