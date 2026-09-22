using System;
using System.Collections.ObjectCollection;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

/// <summary>
/// ViewModel for the category editor dialog (Config -> Categories).
/// Allows renaming task types (e.g., "Red" -> "Urgent").
/// </summary>
public partial class CategoryEditorViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;

    public ObservableCollection<CategoryItem> Categories { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryEditorViewModel"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    public CategoryEditorViewModel(KanbanDbContext db)
    {
        _db = db;

        // Load custom names from database
        var names = db.GetTaskTypeNames();
        foreach (var type in Enum.GetValues<TaskType>())
        {
            var displayName = names.TryGetValue(type, out var name) ? name : type.ToString();
            Categories.Add(new CategoryItem { Type = type, DisplayName = displayName });
        }
    }

    /// <summary>
    /// Saves all custom category names to the database.
    /// </summary>
    public void Save()
    {
        foreach (var cat in Categories)
        {
            var name = string.IsNullOrWhiteSpace(cat.DisplayName) ? cat.Type.ToString() : cat.DisplayName.Trim();
            _db.SaveTaskTypeName(cat.Type, name);
        }
    }

    /// <summary>
    /// Resets all category names to their default enum values.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        foreach (var cat in Categories)
        {
            cat.DisplayName = cat.Type.ToString();
        }
    }
}

/// <summary>
/// Represents a single task type category in the editor.
/// </summary>
public partial class CategoryItem : ObservableObject
{
    public TaskType Type { get; set; }

    [ObservableProperty]
    private string _displayName = string.Empty;
}