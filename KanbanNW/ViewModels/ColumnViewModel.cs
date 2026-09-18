using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.ViewModels;

public partial class ColumnViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private bool _isSystem;

    public ObservableCollection<TaskViewModel> Tasks { get; } = new();

    public static ColumnViewModel FromModel(KanbanColumn column, KanbanDbContext db)
    {
        var vm = new ColumnViewModel
        {
            Id = column.Id,
            Name = column.Name,
            Order = column.Order,
            IsSystem = column.IsSystem
        };
        var tasks = db.GetTasksForColumn(column.Id);
        foreach (var task in tasks)
        {
            vm.Tasks.Add(TaskViewModel.FromModel(task));
        }
        return vm;
    }

    public KanbanColumn ToModel()
    {
        return new KanbanColumn
        {
            Id = Id,
            Name = Name,
            Order = Order,
            IsSystem = IsSystem
        };
    }

    public void ReloadTasks(KanbanDbContext db)
    {
        Tasks.Clear();
        var tasks = db.GetTasksForColumn(Id);
        foreach (var task in tasks)
        {
            Tasks.Add(TaskViewModel.FromModel(task));
        }
    }
}