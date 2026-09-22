namespace KanbanNW.Models;

/// <summary>
/// Represents a column on the kanban board.
/// </summary>
public class KanbanColumn
{
    /// <summary>Unique identifier (primary key).</summary>
    public int Id { get; set; }

    /// <summary>Display name of the column.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display order among columns (lower = leftmost).</summary>
    public int Order { get; set; }

    /// <summary>True for system columns (In Tray, Done, Deleted Tasks) which cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Foreign key to the project this column belongs to.</summary>
    public int ProjectId { get; set; }
}