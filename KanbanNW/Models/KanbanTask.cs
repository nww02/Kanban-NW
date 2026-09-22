using System;

namespace KanbanNW.Models;

/// <summary>
/// Represents a single task/kanban card in the database.
/// </summary>
public class KanbanTask
{
    /// <summary>Unique identifier (primary key).</summary>
    public int Id { get; set; }

    /// <summary>Task title/name.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Timestamp when the task was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Optional due date for the task.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Optional detailed description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Estimated effort in days (default 1).</summary>
    public int EstimatedDays { get; set; } = 1;

    /// <summary>Task type/category (color-coded in UI).</summary>
    public TaskType Type { get; set; } = TaskType.None;

    /// <summary>Foreign key to the column this task belongs to.</summary>
    public int ColumnId { get; set; }

    /// <summary>Whether the task is marked complete.</summary>
    public bool IsComplete { get; set; }

    /// <summary>Display order within the column (0 = top).</summary>
    public int Order { get; set; }
}