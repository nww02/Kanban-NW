using Avalonia;
using Avalonia.Controls;

namespace KanbanNW.Views;

/// <summary>
/// Window for creating or editing a task.
/// DataContext is set to a TaskEditorViewModel.
/// </summary>
public partial class TaskEditorWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskEditorWindow"/> class.
    /// </summary>
    public TaskEditorWindow()
    {
        InitializeComponent();
    }
}