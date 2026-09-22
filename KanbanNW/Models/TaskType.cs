namespace KanbanNW.Models;

/// <summary>
/// Task type/category enumeration. Each type has a distinct color in the UI.
/// Custom display names can be configured in Settings -> Categories.
/// </summary>
public enum TaskType
{
    None,      // Default/unassigned
    Red,       // Typically urgent/critical
    Orange,    // High priority
    Yellow,    // Medium priority
    Green,     // Low priority / features
    Blue,      // Bugs / technical tasks
    Purple,    // Documentation / research
    Black      // Administrative / chores
}