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

public partial class MainViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;

    [ObservableProperty]
    private string _newColumnName = string.Empty;

    [ObservableProperty]
    private bool _showDeletedTasks;

    private KanbanColumn? _deletedColumn;

    [ObservableProperty]
    private int _currentProjectId = -1;

    public ObservableCollection<ColumnViewModel> Columns { get; } = new();
    public ObservableCollection<KanbanProject> Projects { get; } = new();

    public Func<TaskEditorViewModel, Task>? ShowTaskEditorCallback { get; set; }
    public Func<string, Task<bool>>? ShowConfirmCallback { get; set; }
    public Func<Task<string?>>? ShowSaveFileCallback { get; set; }

    public ICommand DeleteColumnCommand { get; }
    public ICommand AddTaskToColumnCommand { get; }
    public ICommand EditTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand MoveTaskToDoneCommand { get; }
    public ICommand EmptyQueueCommand { get; }
    public ICommand ExportProjectCommand { get; }

    public MainViewModel(KanbanDbContext db)
    {
        _db = db;

        DeleteColumnCommand = new AsyncRelayCommand<ColumnViewModel>(OnDeleteColumn, c => c != null && !c.IsSystem);
        AddTaskToColumnCommand = new AsyncRelayCommand<ColumnViewModel>(OnAddTaskToColumn, c => c != null);
        EditTaskCommand = new AsyncRelayCommand<TaskViewModel>(OnEditTask, t => t != null);
        DeleteTaskCommand = new AsyncRelayCommand<TaskViewModel>(OnDeleteTask, t => t != null);
        MoveTaskToDoneCommand = new RelayCommand<TaskViewModel>(OnMoveTaskToDone, t => t != null && !t.IsComplete);
        EmptyQueueCommand = new AsyncRelayCommand(OnEmptyQueue, () => CurrentProjectId > 0 && _db.GetDeletedTaskCount(CurrentProjectId) > 0);
        ExportProjectCommand = new AsyncRelayCommand(OnExportProject, () => CurrentProjectId > 0);

        LoadProjects();
    }

    public void LoadProjects()
    {
        Projects.Clear();
        foreach (var p in _db.GetAllProjects())
            Projects.Add(p);
        if (CurrentProjectId <= 0 && Projects.Count > 0)
            CurrentProjectId = Projects[0].Id;
    }

    partial void OnCurrentProjectIdChanged(int value)
    {
        if (value <= 0) return;
        LoadColumnsForProject(value);
    }

    public void LoadColumnsForProject(int projectId)
    {
        Columns.Clear();
        _deletedColumn = null;
        var columns = _db.GetColumnsForProject(projectId);
        foreach (var col in columns)
        {
            if (col.Order == 1000)
            {
                _deletedColumn = col;
                continue;
            }
            Columns.Add(ColumnViewModel.FromModel(col, _db));
        }
        if (ShowDeletedTasks && _deletedColumn != null)
            Columns.Add(ColumnViewModel.FromModel(_deletedColumn, _db));
    }

    public int AddColumnWithName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || CurrentProjectId <= 0) return 0;
        var id = _db.CreateColumn(name.Trim(), CurrentProjectId);
        LoadColumnsForProject(CurrentProjectId);
        return id;
    }

    partial void OnShowDeletedTasksChanged(bool value)
    {
        if (CurrentProjectId <= 0) return;
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

    public void SwitchToProject(int projectId)
    {
        if (projectId == CurrentProjectId || projectId <= 0) return;
        CurrentProjectId = projectId;
    }

    private async Task OnDeleteColumn(ColumnViewModel? column)
    {
        if (column == null || column.IsSystem) return;
        if (ShowConfirmCallback != null)
        {
            var msg = string.Format(
                "Are you sure you want to delete the column {0}? All tasks will be deleted.",
                column.Name);
            var confirmed = await ShowConfirmCallback(msg);
            if (!confirmed) return;
        }
        _db.DeleteColumn(column.Id);
        Columns.Remove(column);
    }

    private async Task OnAddTaskToColumn(ColumnViewModel? column)
    {
        if (column == null || ShowTaskEditorCallback == null || CurrentProjectId <= 0) return;
        var doneCol = _db.GetDoneColumn(CurrentProjectId);
        if (doneCol != null && column.Id == doneCol.Id)
            return;
        var editor = new TaskEditorViewModel(_db, null, column.Id);
        await ShowTaskEditorCallback(editor);
        if (editor.IsSaved && editor.CreatedTask != null)
            column.Tasks.Add(TaskViewModel.FromModel(editor.CreatedTask));
    }

    private async Task OnEditTask(TaskViewModel? task)
    {
        if (task == null || ShowTaskEditorCallback == null) return;
        var modelTask = _db.GetTaskById(task.Id);
        if (modelTask == null) return;
        var editor = new TaskEditorViewModel(_db, modelTask, task.ColumnId);
        await ShowTaskEditorCallback(editor);
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

    private async Task OnEmptyQueue()
    {
        if (CurrentProjectId <= 0) return;
        if (ShowConfirmCallback != null)
        {
            var count = _db.GetDeletedTaskCount(CurrentProjectId);
            var confirmed = await ShowConfirmCallback("Permanently delete tasks in queue?");
            if (!confirmed) return;
        }
        _db.EmptyDeletedColumn(CurrentProjectId);
        if (_deletedColumn != null)
        {
            var existingCol = GetColumnById(_deletedColumn.Id);
            if (existingCol != null)
            {
                Columns.Remove(existingCol);
                Columns.Add(ColumnViewModel.FromModel(_deletedColumn, _db));
            }
        }
        if (EmptyQueueCommand is AsyncRelayCommand relay)
            relay.NotifyCanExecuteChanged();
    }

    private void OnMoveTaskToDone(TaskViewModel? task)
    {
        if (task == null || CurrentProjectId <= 0) return;
        var doneCol = _db.GetDoneColumn(CurrentProjectId);
        if (doneCol == null) return;
        task.IsComplete = true;
        task.ColumnId = doneCol.Id;
        var newOrder = _db.GetTasksForColumn(doneCol.Id).Count;
        _db.MoveTaskToColumn(task.Id, doneCol.Id, newOrder);
        RemoveTaskFromAllColumns(task.Id);
        var doneVm = GetColumnById(doneCol.Id);
        doneVm?.Tasks.Add(task);
    }

    public void MoveTaskToColumnAtPosition(int taskId, int targetColumnId, int insertIndex)
    {
        var task = _db.GetTaskById(taskId);
        if (task == null) return;

        var existing = _db.GetTasksForColumn(targetColumnId);
        var sameColumn = task.ColumnId == targetColumnId;

        if (sameColumn)
        {
            // Remove the task from its current position
            var oldIndex = existing.FindIndex(t => t.Id == taskId);
            if (oldIndex >= 0)
            {
                existing.RemoveAt(oldIndex);
                // If dropping below where it was, adjust insert index
                if (insertIndex > oldIndex)
                    insertIndex--;
            }
        }

        // Clamp
        if (insertIndex < 0) insertIndex = 0;
        if (insertIndex > existing.Count) insertIndex = existing.Count;

        // Insert at the desired position
        task.ColumnId = targetColumnId;
        existing.Insert(insertIndex, task);

        // Renumber all tasks in the column
        for (int i = 0; i < existing.Count; i++)
            _db.MoveTaskToColumn(existing[i].Id, targetColumnId, i);

        // Rebuild the ViewModel column's task list
        RemoveTaskFromAllColumns(taskId);
        var targetVm = GetColumnById(targetColumnId);
        targetVm?.ReloadTasks(_db);
    }

    private void RemoveTaskFromAllColumns(int taskId)
    {
        foreach (var col in Columns)
        {
            var found = FindTaskInColumn(col, taskId);
            if (found != null)
                col.Tasks.Remove(found);
        }
    }

    private static TaskViewModel? FindTaskInColumn(ColumnViewModel column, int taskId)
    {
        foreach (var t in column.Tasks)
            if (t.Id == taskId) return t;
        return null;
    }

    private ColumnViewModel? GetColumnById(int id)
    {
        foreach (var col in Columns)
            if (col.Id == id) return col;
        return null;
    }


    private async Task OnExportProject()
    {
        if (CurrentProjectId <= 0) return;

        var filePath = ShowSaveFileCallback != null ? await ShowSaveFileCallback() : null;
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            var project = _db.GetAllProjects().FirstOrDefault(p => p.Id == CurrentProjectId);
            var projectName = project?.Name ?? "Project";

            var allColumns = _db.GetColumnsForProject(CurrentProjectId);
            var columnNames = allColumns.ToDictionary(c => c.Id, c => c.Name);
            var allTasks = allColumns.SelectMany(c => _db.GetTasksForColumn(c.Id))
                                     .OrderBy(t => t.ColumnId).ThenBy(t => t.Order).ToList();

            var sb = new StringBuilder();
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

    private static string CsvEscape(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }
}
