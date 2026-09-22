using System;

namespace KanbanNW.Models;

/// <summary>
/// A comment attached to a task.
/// </summary>
public class Comment
{
    /// <summary>Unique identifier (primary key).</summary>
    public int Id { get; set; }

    /// <summary>Comment text content.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Timestamp when the comment was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Foreign key to the parent task.</summary>
    public int TaskId { get; set; }
}