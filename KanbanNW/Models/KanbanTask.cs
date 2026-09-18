using System;

namespace KanbanNW.Models;

public class KanbanTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int EstimatedDays { get; set; } = 1;
    public TaskType Type { get; set; } = TaskType.None;
    public int ColumnId { get; set; }
    public bool IsComplete { get; set; }
    public int Order { get; set; }
}