using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace KanbanNW.Views;

public partial class ColorPickerDialog : Window
{
    public Color SelectedColor => ColorViewControl.Color;

    public ColorPickerDialog()
    {
        InitializeComponent();
    }

    public ColorPickerDialog(Color initialColor) : this()
    {
        ColorViewControl.Color = initialColor;
        ColorViewControl.ColorChanged += (_, args) => { };  // color updates live in the ColorView
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    public static async System.Threading.Tasks.Task<Color?> PickAsync(Window owner, Color initialColor)
    {
        var dialog = new ColorPickerDialog(initialColor);
        var result = await dialog.ShowDialog<bool>(owner);
        return result ? dialog.SelectedColor : null;
    }
}