using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

public partial class CategoryEditorViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;

    public ObservableCollection<CategoryItem> Categories { get; } = new();

    public CategoryEditorViewModel(KanbanDbContext db)
    {
        _db = db;
        var names = db.GetTaskTypeNames();
        foreach (var type in Enum.GetValues<TaskType>())
        {
            var displayName = names.TryGetValue(type, out var name) ? name : type.ToString();
            Categories.Add(new CategoryItem { Type = type, DisplayName = displayName });
        }
    }

    public void Save()
    {
        foreach (var cat in Categories)
        {
            var name = string.IsNullOrWhiteSpace(cat.DisplayName) ? cat.Type.ToString() : cat.DisplayName.Trim();
            _db.SaveTaskTypeName(cat.Type, name);
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        foreach (var cat in Categories)
        {
            cat.DisplayName = cat.Type.ToString();
        }
    }
}

public partial class CategoryItem : ObservableObject
{
    public TaskType Type { get; set; }

    [ObservableProperty]
    private string _displayName = string.Empty;
}