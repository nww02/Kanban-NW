using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KanbanNW.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

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
