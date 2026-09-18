using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Converters;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

public partial class TaskEditorViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;
    private readonly int _defaultColumnId;
    private readonly int? _existingTaskId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _dueDate;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TaskType _type = TaskType.None;

    [ObservableProperty]
    private TaskTypeDisplay? _selectedTypeItem;

    [ObservableProperty]
    private string _newCommentText = string.Empty;

    [ObservableProperty]
    private int _estimatedDays = 1;

    [ObservableProperty]
    private bool _isSaved;

    // Set by the window code-behind so Save/Cancel can close the dialog
    public Action? CloseAction { get; set; }

    public KanbanTask? CreatedTask { get; private set; }
    public ObservableCollection<Comment> Comments { get; } = new();
    public bool IsEditing => _existingTaskId.HasValue;

    // For the type combo box (shows custom display names)
    public TaskTypeDisplay[] TaskTypes { get; }

    public TaskEditorViewModel(KanbanDbContext db, KanbanTask? existingTask, int defaultColumnId)
    {
        _db = db;
        _defaultColumnId = defaultColumnId;
        _existingTaskId = existingTask?.Id;

        // Load custom type display names from the cache
        TaskTypes = Enum.GetValues<TaskType>()
            .Select(t => new TaskTypeDisplay { Type = t, DisplayName = TaskTypeNameCache.GetDisplayName(t) })
            .ToArray();

        if (existingTask != null)
        {
            Title = existingTask.Title;
            DueDate = existingTask.DueDate.HasValue ? new DateTimeOffset(existingTask.DueDate.Value) : null;
            Description = existingTask.Description;
            Type = existingTask.Type;
            var comments = db.GetComments(existingTask.Id);
            foreach (var c in comments)
                Comments.Add(c);
        }

        // Set selected type item to match the current Type
        SelectedTypeItem = TaskTypes.FirstOrDefault(t => t.Type == Type) ?? TaskTypes.FirstOrDefault();
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return;

        var task = new KanbanTask
        {
            Id = _existingTaskId ?? 0,
            Title = Title.Trim(),
            CreatedAt = DateTime.Now,
            DueDate = DueDate?.DateTime,
            Description = Description.Trim(),
            Type = SelectedTypeItem?.Type ?? Type,
            ColumnId = _defaultColumnId,
            IsComplete = false,
            Order = 0,
            EstimatedDays = EstimatedDays
        };

        if (_existingTaskId.HasValue)
        {
            task.ColumnId = _db.GetTaskById(task.Id)?.ColumnId ?? _defaultColumnId;
            _db.UpdateTask(task);
        }
        else
        {
            var id = _db.CreateTask(task);
            task.Id = id;

            // Add a "Task Created" comment
            var createdComment = new Comment
            {
                Text = "Task created",
                CreatedAt = DateTime.Now,
                TaskId = task.Id
            };
            _db.AddComment(createdComment);
            Comments.Add(createdComment);

            // Save any pending comments with the new task ID
            foreach (var comment in Comments)
            {
                if (comment.TaskId == 0)
                {
                    comment.TaskId = task.Id;
                    _db.AddComment(comment);
                }
            }
        }

        CreatedTask = task;
        IsSaved = true;
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        IsSaved = false;
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void ClearDueDate()
    {
        DueDate = null;
    }

    [RelayCommand]
    private void AddComment()
    {
        if (string.IsNullOrWhiteSpace(NewCommentText))
            return;

        // If editing, save directly to DB; if creating, hold in memory until save
        if (_existingTaskId.HasValue)
        {
            var comment = new Comment
            {
                Text = NewCommentText.Trim(),
                CreatedAt = DateTime.Now,
                TaskId = _existingTaskId.Value
            };
            _db.AddComment(comment);
            Comments.Add(comment);
        }
        else
        {
            // For new tasks, comments are added after creation
            // Hold in memory for now
            Comments.Add(new Comment
            {
                Text = NewCommentText.Trim(),
                CreatedAt = DateTime.Now,
                TaskId = 0
            });
        }

        NewCommentText = string.Empty;
    }
}