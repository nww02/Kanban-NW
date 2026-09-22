using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

/// <summary>
/// Main application ViewModel. Manages projects, columns, tasks, and coordinates
/// all user interactions with the database.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;

    [ObservableProperty]
    private string _newColumnName = string.Empty;

    [ObservableProperty]
    private bool _showDeletedTasks;

    /// <summary>The Deleted Tasks column (loaded separately since it's hidden by default).</summary>
    private KanbanColumn? _deletedColumn;

    /// <summary>Currently selected project ID (-1 means none selected).</summary>
    [ObservableProperty]
    private int _currentProjectId = -1;

    /// <summary>Columns for the current project (bound to the board UI).</summary>
    public ObservableCollection<ColumnViewModel> Columns { get; } = new();

    /// <summary>All projects (bound to the project tabs at top).</summary>
    public ObservableCollection<KanbanProject> Projects { get; } = new();

    // Callbacks injected by MainWindow for dialog interactions
    public Func<TaskEditorViewModel, Task>? ShowTaskEditorCallback { get; set; }
    public Func<string, Task<bool>>? ShowConfirmCallback { get; set; }
    public Func<Task<string?>>? ShowSaveFileCallback { get; set; }

    // Commands bound from UI
    public ICommand DeleteColumnCommand { get; }
    public ICommand AddTaskToColumnCommand { get; }
    public ICommand EditTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand MoveTaskToDoneCommand { get; }
    public ICommand EmptyQueueCommand { get; }
    public ICommand ExportProjectCommand { get; }

    /// <summary>
    /// Constructor. Initializes commands and loads projects from database.
    /// </summary>
    public MainViewModel(KanbanDbContext db)
    {
        _db = db;

        // Initialize all commands with their execute methods and can-execute conditions
        DeleteColumnCommand = new AsyncRelayCommand<ColumnViewModel>(OnDeleteColumn, c => c != null && !c.IsSystem);
        AddTaskToColumnCommand = new AsyncRelayCommand<ColumnViewModel>(OnAddTaskToColumn, c => c != null);
        EditTaskCommand = new AsyncRelayCommand<TaskViewModel>(OnEditTask, t => t != null);
        DeleteTaskCommand = new AsyncRelayCommand<TaskViewModel>(OnDeleteTask, t => t != null);
        MoveTaskToDoneCommand = new RelayCommand<TaskViewModel>(OnMoveTaskToDone, t => t != null && !t.IsComplete);
        EmptyQueueCommand = new AsyncRelayCommand(OnEmptyQueue, () => CurrentProjectId > 0 && _db.GetDeletedTaskCount(CurrentProjectId) > 0);
        ExportProjectCommand = new AsyncRelayCommand(OnExportProject, () => CurrentProjectId > 0);

        LoadProjects();
    }

    /// <summary>
    /// Loads all projects from database into the Projects collection.
    /// </summary>
    public void LoadProjects()
    {
        Projects.Clear();
        foreach (var p in _db.GetAllProjects())
            Projects.Add(p);

        // Auto-select first project if none selected
        if (CurrentProjectId <= 0 && Projects.Count > 0)
            CurrentProjectId = Projects[0].Id;
    }

    /// <summary>
    /// Called when CurrentProjectId changes. Loads columns for the new project.
    /// </summary>
    partial void OnCurrentProjectIdChanged(int value)
    {
        if (value <= 0) return;
        LoadColumnsForProject(value);
    }

    /// <summary>
    /// Loads all columns for the given project from database into the Columns collection.
    /// Handles system columns (In Tray, Done, Deleted Tasks) specially.
    /// </summary>
    public void LoadColumnsForProject(int projectId)
    {
        Columns.Clear();
        _deletedColumn = null;

        var columns = _db.GetColumnsForProject(projectId);
        foreach (var col in columns)
        {
            if (col.Order == 1000)  // Deleted Tasks column (hidden by default)
            {
                _deletedColumn = col;
                continue;
            }
            Columns.Add(ColumnViewModel.FromModel(col, _db));
        }

        // Add Deleted Tasks column if "Show Deleted" is enabled
        if (ShowDeletedTasks && _deletedColumn != null)
            Columns.Add(ColumnViewModel.FromModel(_deletedColumn, _db));
    }

    /// <summary>
    /// Creates a new column with the given name in the current project.
    /// </summary>
    /// <returns>The new column's ID, or 0 if invalid.</returns>
    public int AddColumnWithName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || CurrentProjectId <= 0) return 0;
        var id = _db.CreateColumn(name.Trim(), CurrentProjectId);
        LoadColumnsForProject(CurrentProjectId);
        return id;
    }

    /// <summary>
    /// Called when ShowDeletedTasks property changes. Adds/removes the Deleted Tasks column.
    /// </summary>
    partial void OnShowDeletedTasksChanged(bool value)
    {
        if (CurrentProjectId <= 0) return;

        // Ensure deleted column exists (lazy creation)
        if (_deletedColumn == null)
            _deletedColumn = _db.GetOrCreateDeletedColumn(CurrentProjectId);

        if (value)
        {
            Columns.Add(ColumnViewModel.FromModel(_deletedColumn, _db));
        }
        else
        {
            var found = GetColumnById(_deletedColumn!.Id);
            if (found != null)
                Columns.Remove(found);
        }
    }

    /// <summary>
    /// Switches to a different project by ID.
    /// </summary>
    public void SwitchToProject(int projectId)
    {
        if (projectId == CurrentProjectId || projectId <= 0) return;
        CurrentProjectId = projectId;
    }

    /// <summary>
    /// Deletes a user-created column after confirmation. Moves its tasks to In Tray.
    /// </summary>
    private async Task OnDeleteColumn(ColumnViewModel? column)
    {
        if (column == null || column.IsSystem) return;

        if (ShowConfirmCallback != null)
        {
            var msg = string.Format(
                "Are you sure you want to delete the column {0}? All tasks will be moved to In Tray.",
                column.Name);
            var confirmed = await ShowConfirmCallback(msg);
            if (!confirmed) return;
        }

        _db.DeleteColumn(column.Id);
        Columns.Remove(column);
    }

    /// <summary>
    /// Opens the task editor to create a new task in the specified column.
    /// </summary>
    private async Task OnAddTaskToColumn(ColumnViewModel? column)
    {
        if (column == null || ShowTaskEditorCallback == null || CurrentProjectId <= 0) return;

        // Prevent adding tasks to the Done column
        var doneCol = _db.GetDoneColumn(CurrentProjectId);
        if (doneCol != null && column.Id == doneCol.Id)
            return;

        var editor = new TaskEditorViewModel(_db, null, column.Id);
        await ShowTaskEditorCallback(editor);

        // If task was saved, add it to the column's task list
        if (editor.IsSaved && editor.CreatedTask != null)
            column.Tasks.Add(TaskViewModel.FromModel(editor.CreatedTask));
    }

    /// <summary>
    /// Opens the task editor to edit an existing task.
    /// </summary>
    private async Task OnEditTask(TaskViewModel? task)
    {
        if (task == null || ShowTaskEditorCallback == null) return;

        // Load fresh model from database to ensure we have latest data
        var modelTask = _db.GetTaskById(task.Id);
        if (modelTask == null) return;

        var editor = new TaskEditorViewModel(_db, modelTask, task.ColumnId);
        await ShowTaskEditorCallback(editor);

        // If saved, update the ViewModel with new values
        if (editor.IsSaved && editor.CreatedTask != null)
        {
            task.Title = editor.CreatedTask.Title;
            task.DueDate = editor.CreatedTask.DueDate.HasValue
                ? new DateTimeOffset(editor.CreatedTask.DueDate.Value)
                : null;
            task.Description = editor.CreatedTask.Description;
            task.Type = editor.CreatedTask.Type;
        }
    }

    /// <summary>
    /// Moves a task to the Deleted Tasks queue after confirmation.
    /// </summary>
    private async Task OnDeleteTask(TaskViewModel? task)
    {
        if (task == null || CurrentProjectId <= 0) return;

        if (ShowConfirmCallback != null)
        {
            var msg = string.Format(
                "Are you sure you want to delete task {0}? It will be moved to the Deleted Tasks queue.",
                task.Title);
            var confirmed = await ShowConfirmCallback(msg);
            if (!confirmed) return;
        }

        _db.MoveTaskToDeletedColumn(task.Id, CurrentProjectId);
        RemoveTaskFromAllColumns(task.Id);
    }

    /// <summary>
    /// Permanently deletes all tasks in the Deleted Tasks queue after confirmation.
    /// </summary>
    private async Task OnEmptyQueue()
    {
        if (CurrentProjectId <= 0) return;

        if (ShowConfirmCallback != null)
        {
            var count = _db.GetDeletedTaskCount(CurrentProjectId);
            var confirmed = await ShowConfirmCallback("Permanently delete all tasks in the Deleted Tasks queue?");
            if (!confirmed) return;
        }

        _db.EmptyDeletedColumn(CurrentProjectId);

        // Refresh the Deleted Tasks column if it's currently shown
        if (_deletedColumn != null)
        {
            var existingCol = GetColumnById(_deletedColumn.Id);
            if (existingCol != null)
            {
                Columns.Remove(existingCol);
                Columns.Add(ColumnViewModel.FromModel(_deletedColumn, _db));
            }
        }

        // Update command can-execute state
        if (EmptyQueueCommand is AsyncRelayCommand relay)
            relay.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Marks a task as complete and moves it to the Done column.
    /// </summary>
    private void OnMoveTaskToDone(TaskViewModel? task)
    {
        if (task == null || CurrentProjectId <= 0) return;

        var doneCol = _db.GetDoneColumn(CurrentProjectId);
        if (doneCol == null) return;

        task.IsComplete = true;
        task.ColumnId = doneCol.Id;

        // Append to end of Done column
        var newOrder = _db.GetTasksForColumn(doneCol.Id).Count;
        _db.MoveTaskToColumn(task.Id, doneCol.Id, newOrder);

        RemoveTaskFromAllColumns(task.Id);
        var doneVm = GetColumnById(doneCol.Id);
        doneVm?.Tasks.Add(task);
    }

    /// <summary>
    /// Moves a task to a specific column at a specific insert position (for drag-and-drop).
    /// Handles same-column reordering and cross-column moves.
    /// </summary>
    public void MoveTaskToColumnAtPosition(int taskId, int targetColumnId, int insertIndex)
    {
        var task = _db.GetTaskById(taskId);
        if (task == null) return;

        var existing = _db.GetTasksForColumn(targetColumnId);
        var sameColumn = task.ColumnId == targetColumnId;

        if (sameColumn)
        {
            // Remove from current position in the list
            var oldIndex = existing.FindIndex(t => t.Id == taskId);
            if (oldIndex >= 0)
            {
                existing.RemoveAt(oldIndex);
                // Adjust insert index if dropping below original position
                if (insertIndex > oldIndex)
                    insertIndex--;
            }
        }

        // Clamp insert index to valid range
        if (insertIndex < 0) insertIndex = 0;
        if (insertIndex > existing.Count) insertIndex = existing.Count;

        // Insert at desired position
        task.ColumnId = targetColumnId;
        existing.Insert(insertIndex, task);

        // Renumber all tasks sequentially in the database
        for (int i = 0; i < existing.Count; i++)
            _db.MoveTaskToColumn(existing[i].Id, targetColumnId, i);

        // Rebuild the ViewModel column's task list from database
        RemoveTaskFromAllColumns(taskId);
        var targetVm = GetColumnById(targetColumnId);
        targetVm?.ReloadTasks(_db);
    }

    /// <summary>
    /// Removes a task from all column ViewModels' task collections.
    /// </summary>
    private void RemoveTaskFromAllColumns(int taskId)
    {
        foreach (var col in Columns)
        {
            var found = FindTaskInColumn(col, taskId);
            if (found != null)
                col.Tasks.Remove(found);
        }
    }

    /// <summary>
    /// Finds a task by ID within a column's task collection.
    /// </summary>
    private static TaskViewModel? FindTaskInColumn(ColumnViewModel column, int taskId)
    {
        foreach (var t in column.Tasks)
            if (t.Id == taskId) return t;
        return null;
    }

    /// <summary>
    /// Finds a column ViewModel by its ID.
    /// </summary>
    private ColumnViewModel? GetColumnById(int id)
    {
        foreach (var col in Columns)
            if (col.Id == id) return col;
        return null;
    }

    /// <summary>
    /// Exports all tasks in the current project to a CSV file.
    /// </summary>
    private async Task OnExportProject()
    {
        if (CurrentProjectId <= 0) return;

        var filePath = ShowSaveFileCallback != null ? await ShowSaveFileCallback() : null;
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            var project = _db.GetAllProjects().FirstOrDefault(p => p.Id == CurrentProjectId);
            var projectName = project?.Name ?? "Project";

            // Get all columns and tasks ordered by column then position
            var allColumns = _db.GetColumnsForProject(CurrentProjectId);
            var columnNames = allColumns.ToDictionary(c => c.Id, c => c.Name);
            var allTasks = allColumns
                .SelectMany(c => _db.GetTasksForColumn(c.Id))
                .OrderBy(t => t.ColumnId)
                .ThenBy(t => t.Order)
                .ToList();

            var sb = new StringBuilder();
            // CSV header
            sb.AppendLine("Id,Title,CreatedAt,DueDate,Description,Type,Column,IsComplete,Order,EstimatedDays,Comments");

            foreach (var task in allTasks)
            {
                var comments = _db.GetComments(task.Id);
                var commentsText = string.Join(" | ", comments.Select(c => c.Text));
                var csvRow = string.Join(",",
                    task.Id,
                    CsvEscape(task.Title),
                    task.CreatedAt.ToString("yyyy-MM-dd"),
                    task.DueDate?.ToString("yyyy-MM-dd") ?? "",
                    CsvEscape(task.Description ?? ""),
                    task.Type.ToString(),
                    columnNames.TryGetValue(task.ColumnId, out var colName) ? CsvEscape(colName) : $"Column {task.ColumnId}",
                    task.IsComplete ? 1 : 0,
                    task.Order,
                    task.EstimatedDays,
                    CsvEscape(commentsText)
                );
                sb.AppendLine(csvRow);
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            if (ShowConfirmCallback != null)
                await ShowConfirmCallback($"Could not save CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Escapes a field for CSV output (wraps in quotes if contains comma, quote, or newline).
    /// </summary>
    private static string CsvEscape(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }
}