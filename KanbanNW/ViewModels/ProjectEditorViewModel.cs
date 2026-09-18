using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

public partial class ProjectEditorViewModel : ViewModelBase
{
    private readonly KanbanDbContext _db;

    public ObservableCollection<ProjectItem> Projects { get; } = new();
    public string[] ColorOptions { get; } = new[] { "#2C3E50", "#34495E", "#1A5276", "#922B21", "#1E8449", "#B7950B", "#6C3483", "#2E86C1", "#616A6B", "#D35400", "#27AE60" };

    public ProjectEditorViewModel(KanbanDbContext db)
    {
        _db = db;
        Reload();
    }

    public void Reload()
    {
        Projects.Clear();
        foreach (var p in _db.GetAllProjects())
        {
            Projects.Add(new ProjectItem { Id = p.Id, Name = p.Name, Color = p.Color });
        }
    }

    [RelayCommand]
    private void AddProject()
    {
        var count = 1;
        while (_db.GetAllProjects().Any(p => p.Name == $"New Project {count}"))
            count++;
        var id = _db.CreateProject($"New Project {count}");
        Projects.Add(new ProjectItem { Id = id, Name = $"New Project {count}", Color = "#2C3E50" });
    }

    [RelayCommand]
    private void DeleteProject(ProjectItem? item)
    {
        if (item == null) return;
        if (Projects.Count <= 1) return; // never delete last
        _db.DeleteProject(item.Id);
        Projects.Remove(item);
    }

    public void SaveChanges()
    {
        foreach (var item in Projects)
        {
            var name = string.IsNullOrWhiteSpace(item.Name) ? "Untitled" : item.Name.Trim();
            _db.UpdateProject(item.Id, name, item.Color);
        }
    }
}

public partial class ProjectItem : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = "#2C3E50";
}