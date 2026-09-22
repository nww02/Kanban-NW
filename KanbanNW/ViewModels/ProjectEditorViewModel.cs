using System.Collections.ObjectCollection;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

/// <summary>
/// ViewModel for the project editor dialog (Config -> Projects).
/// Supports adding, renaming, deleting projects and changing their colors.
/// </summary>
public partial class ProjectEditorViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;

    public ObservableCollection<ProjectItem> Projects { get; } = new();

    /// <summary>
    /// Predefined color palette for project tabs.
    /// </summary>
    public string[] ColorOptions { get; } = new[]
    {
        "#2C3E50", "#34495E", "#1A5276", "#922B21", "#1E8449",
        "#B7950B", "#6C3483", "#2E86C1", "#616A6B", "#D35400", "#27AE60"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectEditorViewModel"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    public ProjectEditorViewModel(KanbanDbContext db)
    {
        _db = db;
        Reload();
    }

    /// <summary>
    /// Reloads all projects from database into the collection.
    /// </summary>
    public void Reload()
    {
        Projects.Clear();
        foreach (var p in _db.GetAllProjects())
        {
            Projects.Add(new ProjectItem { Id = p.Id, Name = p.Name, Color = p.Color });
        }
    }

    /// <summary>
    /// Adds a new project with default name and color.
    /// </summary>
    [RelayCommand]
    private void AddProject()
    {
        var count = 1;
        while (_db.GetAllProjects().Any(p => p.Name == $"New Project {count}"))
            count++;
        var id = _db.CreateProject($"New Project {count}");
        Projects.Add(new ProjectItem { Id = id, Name = $"New Project {count}", Color = "#2C3E50" });
    }

    /// <summary>
    /// Deletes a project after checking it's not the last one.
    /// </summary>
    /// <param name="item">The project item to delete.</param>
    [RelayCommand]
    private void DeleteProject(ProjectItem? item)
    {
        if (item == null) return;
        if (Projects.Count <= 1) return; // Prevent deleting last project

        _db.DeleteProject(item.Id);
        Projects.Remove(item);
    }

    /// <summary>
    /// Saves all project name and color changes to database.
    /// </summary>
    public void SaveChanges()
    {
        foreach (var item in Projects)
        {
            var name = string.IsNullOrWhiteSpace(item.Name) ? "Untitled" : item.Name.Trim();
            _db.UpdateProject(item.Id, name, item.Color);
        }
    }
}

/// <summary>
/// Represents a project in the editor list.
/// </summary>
public partial class ProjectItem : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = "#2C3E50";
}