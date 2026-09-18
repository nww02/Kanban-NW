using System.Collections.Generic;
using KanbanNW.Data;
using KanbanNW.Models;

namespace KanbanNW.Converters;

/// <summary>
/// Static cache for TaskType display names, loaded from the database at startup.
/// </summary>
public static class TaskTypeNameCache
{
    public static Dictionary<TaskType, string> Names { get; private set; } = new();

    public static void Load(KanbanDbContext db)
    {
        Names = db.GetTaskTypeNames();
    }

    public static void Reload(KanbanDbContext db)
    {
        Load(db);
    }

    public static string GetDisplayName(TaskType type)
    {
        return Names.TryGetValue(type, out var name) ? name : type.ToString();
    }
}