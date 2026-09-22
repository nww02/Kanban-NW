namespace KanbanNW.Models;

/// <summary>
/// Represents a project containing columns and tasks.
/// </summary>
public class KanbanProject
{
    /// <summary>Unique identifier (primary key).</summary>
    public int Id { get; set; }

    /// <summary>Project display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex color for the project tab (e.g., #2C3E50).</summary>
    public string Color { get; set; } = "#2C3E50";
}