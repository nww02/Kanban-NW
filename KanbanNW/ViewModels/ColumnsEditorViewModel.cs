using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

/// <summary>
/// ViewModel for the column editor dialog (Config -> Columns).
/// Supports adding, renaming, deleting, and reordering columns with Save/Cancel.
/// </summary>
public partial class ColumnsEditorViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;
    private readonly int _projectId;

    /// <summary>
    /// Snapshot of original column names from database (key = column ID, value = original name).
    /// Used to detect renames and track which columns were deleted/added.
    /// </summary>
    private readonly Dictionary<int, string> _originalMap = new();

    /// <summary>
    /// Negative IDs assigned to newly added columns (not yet saved to DB).
    /// </summary>
    private int _nextTempId = -1;

    public ObservableCollection<ColumnItem> Columns { get; } = new();

    /// <summary>
    /// Callback to show confirmation dialogs (set by window code-behind).
    /// </summary>
    public Func<string, Task<bool>>? ShowConfirmCallback { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnsEditorViewModel"/> class.
    /// Loads columns from database and snapshots original names.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="projectId">The ID of the project whose columns to edit.</param>
    public ColumnsEditorViewModel(KanbanDbContext db, int projectId)
    {
        _db = db;
        _projectId = projectId;
        LoadFromDb();
    }

    /// <summary>
    /// Loads columns from database and snapshots their names.
    /// </summary>
    private void LoadFromDb()
    {
        Columns.Clear();
        _originalMap.Clear();

        foreach (var c in _db.GetColumnsForProject(_projectId))
        {
            Columns.Add(new ColumnItem { Id = c.Id, Name = c.Name, IsSystem = c.IsSystem });
            _originalMap[c.Id] = c.Name;
        }
    }

    /// <summary>
    /// Adds a new column with a generated unique name.
    /// </summary>
    [RelayCommand]
    private void AddColumn()
    {
        var count = 1;
        var existingNames = Columns.Select(c => c.Name).ToHashSet();
        while (existingNames.Contains($"New Column {count}"))
            count++;

        Columns.Add(new ColumnItem
        {
            Id = _nextTempId--,
            Name = $"New Column {count}",
            IsSystem = false
        });
    }

    /// <summary>
    /// Deletes a column after user confirmation.
    /// </summary>
    /// <param name="item">The column item to delete.</param>
    [RelayCommand]
    private async Task DeleteColumn(ColumnItem? item)
    {
        if (item == null || item.IsSystem) return;

        if (ShowConfirmCallback != null)
        {
            var msg = $"Are you sure you want to delete column \"{item.Name}\"?\nAll tasks will be moved to In Tray.";
            var confirmed = await ShowConfirmCallback(msg);
            if (!confirmed) return;
        }

        Columns.Remove(item);
    }

    /// <summary>
    /// Commits all changes (adds, renames, deletes) to the database.
    /// </summary>
    public void Save()
    {
        var currentIds = Columns.Where(c => c.Id > 0).Select(c => c.Id).ToHashSet();

        // 1. Delete columns removed from the list
        foreach (var kvp in _originalMap)
        {
            if (currentIds.Contains(kvp.Key)) continue;

            // Move tasks to In Tray before deleting column
            var inTray = _db.GetInTrayColumn(_projectId);
            if (inTray != null)
            {
                var tasks = _db.GetTasksForColumn(kvp.Key);
                foreach (var task in tasks)
                {
                    var nextOrder = _db.GetTasksForColumn(inTray.Id).Count;
                    _db.MoveTaskToColumn(task.Id, inTray.Id, nextOrder);
                }
            }
            _db.DeleteColumn(kvp.Key);
        }

        // 2. Create new columns (those with temporary negative IDs)
        foreach (var item in Columns)
        {
            if (item.Id >= 0) continue;

            var name = string.IsNullOrWhiteSpace(item.Name) ? "Untitled" : item.Name.Trim();
            var newId = _db.CreateColumn(name, _projectId);
            item.Id = newId;
        }

        // 3. Rename changed columns
        foreach (var item in Columns)
        {
            if (item.Id <= 0) continue;

            if (!_originalMap.TryGetValue(item.Id, out var originalName)) continue;
            var trimmed = (item.Name ?? "").Trim();
            if (trimmed == originalName) continue;

            _db.RenameColumn(item.Id, trimmed);
        }
    }
}

/// <summary>
/// Represents a single column in the editor list.
/// </summary>
public partial class ColumnItem : ObservableObject
{
    public int Id { get; set; }
    public bool IsSystem { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;
}