using System;
using CommunityToolkit.Mvvm.ComponentModel;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

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

    [ObservableProperty]
    private bool _isDragPlaceholder;

    [ObservableProperty]
    private double _placeholderHeight = 60;

    public Avalonia.Media.ISolidColorBrush? DueDateColor
    {
        get
        {
            if (DueDate == null) return null;
            var now = DateTimeOffset.Now;
            var estimatedEnd = now.AddDays(EstimatedDays);
            if (DueDate.Value < now)
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF5350")); // past = red
            if (DueDate.Value <= estimatedEnd)
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFA726")); // within estimate = orange
            return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#66BB6A")); // future = green
        }
    }

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