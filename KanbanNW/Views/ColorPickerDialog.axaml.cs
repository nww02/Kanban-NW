using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace KanbanNW.Views;

/// <summary>
/// Color picker dialog using Avalonia's built-in ColorView control.
/// </summary>
public partial class ColorPickerDialog : Window
{
    /// <summary>
    /// The currently selected color.
    /// </summary>
    public Color SelectedColor => ColorViewControl.Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorPickerDialog"/> class.
    /// </summary>
    public ColorPickerDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorPickerDialog"/> class with an initial color.
    /// </summary>
    /// <param name="initialColor">The initial color to display.</param>
    public ColorPickerDialog(Color initialColor) : this()
    {
        ColorViewControl.Color = initialColor;
        ColorViewControl.ColorChanged += (_, args) => { }; // Color updates live in the ColorView
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Opens the color picker and returns the selected color, or null if cancelled.
    /// </summary>
    /// <param name="owner">The owner window for modal dialog.</param>
    /// <param name="initialColor">The initial color to display.</param>
    /// <returns>The selected color, or null if cancelled.</param>
    public static async System.Threading.Tasks.Task<Color?> PickAsync(Window owner, Color initialColor)
    {
        var dialog = new ColorPickerDialog(initialColor);
        var result = await dialog.ShowDialog<bool>(owner);
        return result ? dialog.SelectedColor : null;
    }
}