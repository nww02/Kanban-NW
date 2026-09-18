using System.ComponentModel;

namespace KanbanNW.Models;

public class TaskTypeDisplay
{
    public TaskType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}