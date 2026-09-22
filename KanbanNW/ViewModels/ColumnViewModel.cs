using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

/// <summary>
/// ViewModel for a single column on the kanban board.
/// Contains the column's metadata and its collection of tasks.
/// </summary>
public partial class ColumnViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private bool _isSystem;

    /// <summary>
    /// Tasks belonging to this column (bound to ListBox in UI).
    /// </summary>
    public ObservableCollection<TaskViewModel> Tasks { get; } = new();

    /// <summary>
    /// Creates a ColumnViewModel from a database model.
    /// </summary>
    /// <param name="column">The database column model.</param>
    /// <param name="db">The database context to load tasks from.</param>
    /// <returns>A new <see cref="ColumnViewModel"/> populated with the column's tasks.</returns>
    public static ColumnViewModel FromModel(KanbanColumn column, KanbanDbContext db)
    {
        var vm = new ColumnViewModel
        {
            Id = column.Id,
            Name = column.Name,
            Order = column.Order,
            IsSystem = column.IsSystem
        };

        // Load all tasks for this column from database
        var tasks = db.GetTasksForColumn(column.Id);
        foreach (var task in tasks)
        {
            vm.Tasks.Add(TaskViewModel.FromModel(task));
        }

        return vm;
    }

    /// <summary>
    /// Converts this ViewModel back to a database model.
    /// </summary>
    /// <returns>A <see cref="KanbanColumn"/> model with the same data.</returns>
    public KanbanColumn ToModel()
    {
        return new KanbanColumn
        {
            Id = Id,
            Name = Name,
            Order = Order,
            IsSystem = IsSystem
        };
    }

    /// <summary>
    /// Reloads tasks from database (used after drag-and-drop reordering).
    /// </summary>
    /// <param name="db">The database context to load tasks from.</param>
    public void ReloadTasks(KanbanDbContext db)
    {
        Tasks.Clear();
        var tasks = db.GetTasksForColumn(Id);
        foreach (var task in tasks)
        {
            Tasks.Add(TaskViewModel.FromModel(task));
        }
    }
}