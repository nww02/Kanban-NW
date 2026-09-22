using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Converters;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

/// <summary>
/// ViewModel for the task editor dialog (create/edit task).
/// Handles validation, comments, and saving to database.
/// </summary>
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

    /// <summary>
    /// Callback to close the dialog window. Set by the window code-behind.
    /// </summary>
    public Action? CloseAction { get; set; }

    /// <summary>
    /// The task that was created or updated (set on Save).
    /// </summary>
    public KanbanTask? CreatedTask { get; private set; }

    /// <summary>
    /// Comments associated with this task (loaded from DB or held in memory for new tasks).
    /// </summary>
    public ObservableCollection<Comment> Comments { get; } = new();

    /// <summary>
    /// True if editing an existing task (vs creating new).
    /// </summary>
    public bool IsEditing => _existingTaskId.HasValue;

    /// <summary>
    /// Available task types for the combo box (with custom display names).
    /// </summary>
    public TaskTypeDisplay[] TaskTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskEditorViewModel"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="existingTask">Existing task to edit, or null for new task.</param>
    /// <param name="defaultColumnId">Column ID to assign the new task to.</param>
    public TaskEditorViewModel(KanbanDbContext db, KanbanTask? existingTask, int defaultColumnId)
    {
        _db = db;
        _defaultColumnId = defaultColumnId;
        _existingTaskId = existingTask?.Id;

        // Load task types with custom display names from cache
        TaskTypes = Enum.GetValues<TaskType>()
            .Select(t => new TaskTypeDisplay { Type = t, DisplayName = TaskTypeNameCache.GetDisplayName(t) })
            .ToArray();

        if (existingTask != null)
        {
            // Editing existing task: load its data
            Title = existingTask.Title;
            DueDate = existingTask.DueDate.HasValue ? new DateTimeOffset(existingTask.DueDate.Value) : null;
            Description = existingTask.Description;
            Type = existingTask.Type;

            // Load existing comments from database
            var comments = db.GetComments(existingTask.Id);
            foreach (var c in comments)
                Comments.Add(c);
        }

        // Set combo box selection to match current Type
        SelectedTypeItem = TaskTypes.FirstOrDefault(t => t.Type == Type) ?? TaskTypes.FirstOrDefault();
    }

    /// <summary>
    /// Saves the task to the database (create or update).
    /// </summary>
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
            // Editing existing task: preserve its column
            task.ColumnId = _db.GetTaskById(task.Id)?.ColumnId ?? _defaultColumnId;
            _db.UpdateTask(task);
        }
        else
        {
            // Creating new task
            var id = _db.CreateTask(task);
            task.Id = id;

            // Add "Task Created" comment
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

    /// <summary>
    /// Cancels the dialog without saving.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        IsSaved = false;
        CloseAction?.Invoke();
    }

    /// <summary>
    /// Clears the due date.
    /// </summary>
    [RelayCommand]
    private void ClearDueDate()
    {
        DueDate = null;
    }

    /// <summary>
    /// Adds a comment (saves to DB if editing, holds in memory if creating).
    /// </summary>
    [RelayCommand]
    private void AddComment()
    {
        if (string.IsNullOrWhiteSpace(NewCommentText))
            return;

        if (_existingTaskId.HasValue)
        {
            // Editing: save comment directly to database
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
            // Creating: hold comment in memory until task is saved
            Comments.Add(new Comment
            {
                Text = NewCommentText.Trim(),
                CreatedAt = DateTime.Now,
                TaskId = 0  // Will be updated after task creation
            });
        }

        NewCommentText = string.Empty;
    }
}