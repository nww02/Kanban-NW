using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

public partial class ColumnsEditorViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;
    private readonly int _projectId;
    private readonly Dictionary<int, string> _originalMap = new();
    private int _nextTempId = -1;

    public ObservableCollection<ColumnItem> Columns { get; } = new();

    // Callback for confirmation dialog
    public Func<string, Task<bool>>? ShowConfirmCallback { get; set; }

    public ColumnsEditorViewModel(KanbanDbContext db, int projectId)
    {
        _db = db;
        _projectId = projectId;
        LoadFromDb();
    }

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

    public void Save()
    {
        var currentIds = Columns.Where(c => c.Id > 0).Select(c => c.Id).ToHashSet();

        // 1. Delete columns removed from the list
        foreach (var kvp in _originalMap)
        {
            if (currentIds.Contains(kvp.Key)) continue;

            // Move tasks to In Tray first
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

        // 2. Create new columns
        foreach (var item in Columns)
        {
            if (item.Id >= 0) continue; // skip existing, only handle temp (negative) ids

            var name = string.IsNullOrWhiteSpace(item.Name) ? "Untitled" : item.Name.Trim();
            var newId = _db.CreateColumn(name, _projectId);
            item.Id = newId;
        }

        // 3. Rename changed columns
        foreach (var item in Columns)
        {
            if (item.Id <= 0) continue; // skip temps
            if (!_originalMap.TryGetValue(item.Id, out var originalName)) continue;
            var trimmed = (item.Name ?? "").Trim();
            if (trimmed == originalName) continue;

            _db.RenameColumn(item.Id, trimmed);
        }
    }
}

public partial class ColumnItem : ObservableObject
{
    public int Id { get; set; }
    public bool IsSystem { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;
}