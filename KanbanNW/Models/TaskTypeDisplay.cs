using System.ComponentModel;

namespace KanbanNW.Models;

/// <summary>
/// Display wrapper for TaskType enum showing custom names in combo boxes.
/// </summary>
public class TaskTypeDisplay
{
    /// <summary>The underlying TaskType enum value.</summary>
    public TaskType Type { get; set; }

    /// <summary>Custom display name (from user settings) or default enum name.</summary>
    public string DisplayName { get; set; } = string.Empty;
}