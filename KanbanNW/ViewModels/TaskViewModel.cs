using System;
using CommunityToolkit.Mvvm.ComponentModel;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

/// <summary>
/// ViewModel for a single task card on the board.
/// </summary>
public partial class TaskViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTimeOffset? _dueDate;

    /// <summary>
    /// Called when DueDate changes to notify UI that DueDateColor may have changed.
    /// </summary>
    partial void OnDueDateChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(DueDateColor));

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TaskType _type = TaskType.Blue;

    [ObservableProperty]
    private int _columnId;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private int _estimatedDays = 1;

    /// <summary>
    /// True if this task is a drag-and-drop placeholder (not a real task).
    /// </summary>
    [ObservableProperty]
    private bool _isDragPlaceholder;

    /// <summary>
    /// Height of the placeholder when dragging (matches the dragged task's height).
    /// </summary>
    [ObservableProperty]
    private double _placeholderHeight = 60;

    /// <summary>
    /// Returns a color brush indicating due date status:
    /// Red = overdue, Orange = within estimated days, Green = future.
    /// </summary>
    public Avalonia.Media.ISolidColorBrush? DueDateColor
    {
        get
        {
            if (DueDate == null) return null;

            var now = DateTimeOffset.Now;
            var estimatedEnd = now.AddDays(EstimatedDays);

            if (DueDate.Value < now)
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF5350")); // Overdue = red
            if (DueDate.Value <= estimatedEnd)
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFA726")); // Within estimate = orange

            return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#66BB6A")); // Future = green
        }
    }

    /// <summary>
    /// Converts this ViewModel to a database model.
    /// </summary>
    /// <returns>A <see cref="KanbanTask"/> model with the same data.</returns>
    public KanbanTask ToModel()
    {
        return new KanbanTask
        {
            Id = Id,
            Title = Title,
            CreatedAt = CreatedAt,
            DueDate = DueDate?.DateTime,
            Description = Description,
            Type = Type,
            ColumnId = ColumnId,
            IsComplete = IsComplete,
            Order = Order,
            EstimatedDays = EstimatedDays
        };
    }

    /// <summary>
    /// Creates a ViewModel from a database model.
    /// </summary>
    /// <param name="task">The database task model to convert.</param>
    /// <returns>A new <see cref="TaskViewModel"/> with the same data.</returns>
    public static TaskViewModel FromModel(KanbanTask task)
    {
        return new TaskViewModel
        {
            Id = task.Id,
            Title = task.Title,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate.HasValue ? new DateTimeOffset(task.DueDate.Value) : null,
            Description = task.Description,
            Type = task.Type,
            ColumnId = task.ColumnId,
            IsComplete = task.IsComplete,
            Order = task.Order,
            EstimatedDays = task.EstimatedDays
        };
    }
}