namespace KanbanNW.Models;

public class KanbanColumn
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsSystem { get; set; }
    public int ProjectId { get; set; }
}