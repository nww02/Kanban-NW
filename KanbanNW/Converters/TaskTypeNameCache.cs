using System.Collections.Generic;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.Converters;

/// <summary>
/// Static cache for TaskType display names, loaded from the database at startup.
/// </summary>
public static class TaskTypeNameCache
{
    /// <summary>
    /// Gets the cached display names for task types.
    /// </summary>
    public static Dictionary<TaskType, string> Names { get; private set; } = new();

    /// <summary>
    /// Loads task type display names from the database into the cache.
    /// </summary>
    /// <param name="db">The database context.</param>
    public static void Load(KanbanDbContext db)
    {
        Names = db.GetTaskTypeNames();
    }

    /// <summary>
    /// Reloads the cache from the database (useful after editing categories).
    /// </summary>
    /// <param name="db">The database context.</param>
    public static void Reload(KanbanDbContext db)
    {
        Load(db);
    }

    /// <summary>
    /// Gets the display name for a task type.
    /// </summary>
    /// <param name="type">The task type.</param>
    /// <returns>The custom display name, or the enum name if not customized.</returns>
    public static string GetDisplayName(TaskType type)
    {
        return Names.TryGetValue(type, out var name) ? name : type.ToString();
    }
}