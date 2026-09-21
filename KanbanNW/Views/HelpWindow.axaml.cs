using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
    }

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
