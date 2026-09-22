using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using KanbanNW.Models;

namespace KanbanNW.Data;

/// <summary>
/// Database context for Kanban-NW. Wraps a SQLite connection and provides
/// CRUD operations for projects, columns, tasks, comments, settings, and categories.
/// </summary>
public class KanbanDbContext : IDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Opens connection to the SQLite database file in LocalApplicationData/KanbanNW/kanban.db.
    /// Creates tables and seeds default data if they don't exist.
    /// </summary>
    public KanbanDbContext()
    {
        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KanbanNW",
            "kanban.db");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        // Enable foreign key cascade deletes (deleting a column deletes its tasks)
        using (var pragmaCmd = _connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
            pragmaCmd.ExecuteNonQuery();
        }

        Initialize();
    }

    /// <summary>
    /// Creates database tables and seeds default data on first run.
    /// Also handles schema migrations for existing databases.
    /// </summary>
    private void Initialize()
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "Projects" (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "Columns" (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                "Order" INTEGER NOT NULL,
                IsSystem INTEGER NOT NULL DEFAULT 0,
                ProjectId INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS "Tasks" (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                DueDate TEXT,
                Description TEXT,
                Type TEXT NOT NULL DEFAULT 'None',
                ColumnId INTEGER NOT NULL,
                IsComplete INTEGER NOT NULL DEFAULT 0,
                "Order" INTEGER NOT NULL DEFAULT 0,
                EstimatedDays INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (ColumnId) REFERENCES "Columns"(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "Comments" (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Text TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                TaskId INTEGER NOT NULL,
                FOREIGN KEY (TaskId) REFERENCES "Tasks"(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "Settings" (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "TaskTypeNames" (
                Type TEXT PRIMARY KEY,
                CustomName TEXT NOT NULL
            );
        """;
        cmd.ExecuteNonQuery();

        // --- Schema migrations for existing databases ---
        // Add EstimatedDays column if missing (legacy databases)
        if (!HasColumn("Tasks", "EstimatedDays"))
        {
            using var estMigrate = _connection.CreateCommand();
            estMigrate.CommandText = "ALTER TABLE \"Tasks\" ADD COLUMN EstimatedDays INTEGER NOT NULL DEFAULT 1";
            estMigrate.ExecuteNonQuery();
        }

        // Add Color column to Projects if missing
        if (!HasColumn("Projects", "Color"))
        {
            using var colorMigrate = _connection.CreateCommand();
            colorMigrate.CommandText = "ALTER TABLE \"Projects\" ADD COLUMN Color TEXT NOT NULL DEFAULT '#2C3E50'";
            colorMigrate.ExecuteNonQuery();
        }

        // Add ProjectId column to Columns if missing
        if (!HasColumn("Columns", "ProjectId"))
        {
            using var migrateCmd = _connection.CreateCommand();
            migrateCmd.CommandText = "ALTER TABLE \"Columns\" ADD COLUMN ProjectId INTEGER NOT NULL DEFAULT 0";
            migrateCmd.ExecuteNonQuery();
        }

        // Seed default project if none exists
        using var projectCountCmd = _connection.CreateCommand();
        projectCountCmd.CommandText = "SELECT COUNT(*) FROM \"Projects\"";
        var projectCount = (long)projectCountCmd.ExecuteScalar()!;
        if (projectCount == 0)
        {
            using var seedProject = _connection.CreateCommand();
            seedProject.CommandText = "INSERT INTO \"Projects\" (Name) VALUES ('Default'); SELECT last_insert_rowid();";
            var defaultProjectId = Convert.ToInt32(seedProject.ExecuteScalar());

            // Migrate existing columns (ProjectId=0) to the default project
            using var updateCols = _connection.CreateCommand();
            updateCols.CommandText = "UPDATE \"Columns\" SET ProjectId = @pid WHERE ProjectId = 0";
            updateCols.Parameters.AddWithValue("@pid", defaultProjectId);
            updateCols.ExecuteNonQuery();
        }

        // Seed system columns (In Tray, Done) for default project
        var defaultProject = GetDefaultProject();
        if (defaultProject != null)
        {
            using var countCmd = _connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM \"Columns\" WHERE ProjectId = @pid AND \"Order\" = 0";
            countCmd.Parameters.AddWithValue("@pid", defaultProject.Id);
            var inTrayCount = (long)countCmd.ExecuteScalar()!;
            if (inTrayCount == 0)
            {
                using var seed1 = _connection.CreateCommand();
                seed1.CommandText = "INSERT INTO \"Columns\" (\"Name\", \"Order\", \"IsSystem\", \"ProjectId\") VALUES ('In Tray', 0, 1, @pid)";
                seed1.Parameters.AddWithValue("@pid", defaultProject.Id);
                seed1.ExecuteNonQuery();
            }

            using var doneCountCmd = _connection.CreateCommand();
            doneCountCmd.CommandText = "SELECT COUNT(*) FROM \"Columns\" WHERE ProjectId = @pid AND \"Order\" = 999";
            doneCountCmd.Parameters.AddWithValue("@pid", defaultProject.Id);
            var doneCount = (long)doneCountCmd.ExecuteScalar()!;
            if (doneCount == 0)
            {
                using var seed2 = _connection.CreateCommand();
                seed2.CommandText = "INSERT INTO \"Columns\" (\"Name\", \"Order\", \"IsSystem\", \"ProjectId\") VALUES ('Done', 999, 1, @pid)";
                seed2.Parameters.AddWithValue("@pid", defaultProject.Id);
                seed2.ExecuteNonQuery();
            }
        }

        // Seed TaskTypeNames if empty
        using var nameCountCmd = _connection.CreateCommand();
        nameCountCmd.CommandText = "SELECT COUNT(*) FROM \"TaskTypeNames\"";
        var nameCount = (long)nameCountCmd.ExecuteScalar()!;
        if (nameCount == 0)
        {
            SeedTaskTypeNames();
        }
    }

    /// <summary>
    /// Checks if a column exists in a table (used for schema migrations).
    /// </summary>
    private bool HasColumn(string table, string column)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(\"" + table + "\")";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1) == column)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Seeds the TaskTypeNames table with default enum names.
    /// </summary>
    private void SeedTaskTypeNames()
    {
        foreach (var type in Enum.GetValues<TaskType>())
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO \"TaskTypeNames\" (Type, CustomName) VALUES (@type, @name)";
            cmd.Parameters.AddWithValue("@type", type.ToString());
            cmd.Parameters.AddWithValue("@name", type.ToString());
            cmd.ExecuteNonQuery();
        }
    }

    // ============================
    // Project CRUD
    // ============================

    /// <summary>Gets all projects ordered by ID.</summary>
    public List<KanbanProject> GetAllProjects()
    {
        var projects = new List<KanbanProject>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Color FROM \"Projects\" ORDER BY Id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            projects.Add(new KanbanProject
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Color = reader.IsDBNull(2) ? "#2C3E50" : reader.GetString(2)
            });
        }
        return projects;
    }

    /// <summary>Gets the first project (default).</summary>
    public KanbanProject? GetDefaultProject()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM \"Projects\" ORDER BY Id LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return new KanbanProject { Id = reader.GetInt32(0), Name = reader.GetString(1) };
        return null;
    }

    /// <summary>Creates a new project and seeds its system columns.</summary>
    public int CreateProject(string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO \"Projects\" (Name) VALUES (@name); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", name);
        var result = Convert.ToInt32(cmd.ExecuteScalar());
        SeedProjectColumns(result);
        return result;
    }

    /// <summary>Updates a project's name and color.</summary>
    public void UpdateProject(int projectId, string name, string color)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE \"Projects\" SET Name = @name, Color = @color WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", projectId);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@color", color);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Deletes a project and all its columns/tasks (if not the last project).</summary>
    public void DeleteProject(int projectId)
    {
        var count = GetAllProjects().Count;
        if (count <= 1) return; // Never delete the last project

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM \"Columns\" WHERE ProjectId = @pid";
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.ExecuteNonQuery();

        using var delCmd = _connection.CreateCommand();
        delCmd.CommandText = "DELETE FROM \"Projects\" WHERE Id = @id";
        delCmd.Parameters.AddWithValue("@id", projectId);
        delCmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Seeds In Tray, Done, and Deleted Tasks columns for a new project.
    /// </summary>
    public void SeedProjectColumns(int projectId)
    {
        using var countCmd = _connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM \"Columns\" WHERE ProjectId = @pid AND \"Order\" = 0";
        countCmd.Parameters.AddWithValue("@pid", projectId);
        var inTrayCount = (long)countCmd.ExecuteScalar()!;
        if (inTrayCount == 0)
        {
            using var seed1 = _connection.CreateCommand();
            seed1.CommandText = "INSERT INTO \"Columns\" (\"Name\", \"Order\", \"IsSystem\", \"ProjectId\") VALUES ('In Tray', 0, 1, @pid)";
            seed1.Parameters.AddWithValue("@pid", projectId);
            seed1.ExecuteNonQuery();
        }

        using var doneCountCmd = _connection.CreateCommand();
        doneCountCmd.CommandText = "SELECT COUNT(*) FROM \"Columns\" WHERE ProjectId = @pid AND \"Order\" = 999";
        doneCountCmd.Parameters.AddWithValue("@pid", projectId);
        var doneCount = (long)doneCountCmd.ExecuteScalar()!;
        if (doneCount == 0)
        {
            using var seed2 = _connection.CreateCommand();
            seed2.CommandText = "INSERT INTO \"Columns\" (\"Name\", \"Order\", \"IsSystem\", \"ProjectId\") VALUES ('Done', 999, 1, @pid)";
            seed2.Parameters.AddWithValue("@pid", projectId);
            seed2.ExecuteNonQuery();
        }

        using var delCountCmd = _connection.CreateCommand();
        delCountCmd.CommandText = "SELECT COUNT(*) FROM \"Columns\" WHERE ProjectId = @pid AND \"Order\" = 1000";
        delCountCmd.Parameters.AddWithValue("@pid", projectId);
        var delCount = (long)delCountCmd.ExecuteScalar()!;
        if (delCount == 0)
        {
            using var seed3 = _connection.CreateCommand();
            seed3.CommandText = "INSERT INTO \"Columns\" (\"Name\", \"Order\", \"IsSystem\", \"ProjectId\") VALUES ('Deleted Tasks', 1000, 1, @pid)";
            seed3.Parameters.AddWithValue("@pid", projectId);
            seed3.ExecuteNonQuery();
        }
    }

    // ============================
    // Column CRUD
    // ============================

    /// <summary>Gets all columns for a project ordered by display order.</summary>
    public List<KanbanColumn> GetColumnsForProject(int projectId)
    {
        var columns = new List<KanbanColumn>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, \"Order\", IsSystem, ProjectId FROM \"Columns\" WHERE ProjectId = @pid ORDER BY \"Order\"";
        cmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            columns.Add(ReadColumn(reader));
        return columns;
    }

    /// <summary>Gets a single column by ID.</summary>
    public KanbanColumn? GetColumnById(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, \"Order\", IsSystem, ProjectId FROM \"Columns\" WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadColumn(reader);
        return null;
    }

    /// <summary>Creates a new user column before the Done column.</summary>
    public int CreateColumn(string name, int projectId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "Columns" ("Name", "Order", "IsSystem", "ProjectId")
            VALUES (@name, (SELECT COALESCE(MAX("Order"), 0) + 1 FROM "Columns" WHERE "Order" < 999 AND ProjectId = @pid), 0, @pid);
            SELECT last_insert_rowid();
        """;
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@pid", projectId);
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    /// <summary>Renames a user column (system columns cannot be renamed).</summary>
    public void RenameColumn(int columnId, string newName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE \"Columns\" SET Name = @name WHERE Id = @id AND \"IsSystem\" = 0";
        cmd.Parameters.AddWithValue("@id", columnId);
        cmd.Parameters.AddWithValue("@name", newName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Deletes a user column (system columns cannot be deleted).</summary>
    public void DeleteColumn(int columnId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM \"Columns\" WHERE Id = @id AND \"IsSystem\" = 0";
        cmd.Parameters.AddWithValue("@id", columnId);
        cmd.ExecuteNonQuery();
    }

    // ============================
    // Task CRUD
    // ============================

    /// <summary>Gets all tasks for a column ordered by display order.</summary>
    public List<KanbanTask> GetTasksForColumn(int columnId)
    {
        var tasks = new List<KanbanTask>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Title, CreatedAt, DueDate, Description, Type, ColumnId, IsComplete, "Order", EstimatedDays
            FROM "Tasks" WHERE ColumnId = @cid ORDER BY "Order"
        """;
        cmd.Parameters.AddWithValue("@cid", columnId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            tasks.Add(ReadTask(reader));
        return tasks;
    }

    /// <summary>Gets a single task by ID.</summary>
    public KanbanTask? GetTaskById(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Title, CreatedAt, DueDate, Description, Type, ColumnId, IsComplete, "Order", EstimatedDays
            FROM "Tasks" WHERE Id = @id
        """;
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTask(reader);
        return null;
    }

    /// <summary>Creates a new task at the end of the specified column.</summary>
    public int CreateTask(KanbanTask task)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "Tasks" (Title, CreatedAt, DueDate, Description, Type, ColumnId, IsComplete, "Order", EstimatedDays)
            VALUES (@title, @created, @due, @desc, @type, @colId, @complete,
                    (SELECT COALESCE(MAX("Order"), 0) + 1 FROM "Tasks" WHERE ColumnId = @colId), @estDays);
            SELECT last_insert_rowid();
        """;
        cmd.Parameters.AddWithValue("@title", task.Title);
        cmd.Parameters.AddWithValue("@created", task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@due", task.DueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", task.Description ?? "");
        cmd.Parameters.AddWithValue("@type", task.Type.ToString());
        cmd.Parameters.AddWithValue("@colId", task.ColumnId);
        cmd.Parameters.AddWithValue("@complete", task.IsComplete ? 1 : 0);
        cmd.Parameters.AddWithValue("@estDays", task.EstimatedDays);
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    /// <summary>Updates all fields of an existing task.</summary>
    public void UpdateTask(KanbanTask task)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE "Tasks" SET Title = @title, DueDate = @due, Description = @desc,
                             Type = @type, ColumnId = @colId, IsComplete = @complete, "Order" = @order, EstimatedDays = @estDays
            WHERE Id = @id
        """;
        cmd.Parameters.AddWithValue("@id", task.Id);
        cmd.Parameters.AddWithValue("@title", task.Title);
        cmd.Parameters.AddWithValue("@due", task.DueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", task.Description ?? "");
        cmd.Parameters.AddWithValue("@type", task.Type.ToString());
        cmd.Parameters.AddWithValue("@colId", task.ColumnId);
        cmd.Parameters.AddWithValue("@complete", task.IsComplete ? 1 : 0);
        cmd.Parameters.AddWithValue("@estDays", task.EstimatedDays);
        cmd.Parameters.AddWithValue("@order", task.Order);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Moves a task to a different column with a new order.</summary>
    public void MoveTaskToColumn(int taskId, int newColumnId, int newOrder)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE \"Tasks\" SET ColumnId = @colId, \"Order\" = @order WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", taskId);
        cmd.Parameters.AddWithValue("@colId", newColumnId);
        cmd.Parameters.AddWithValue("@order", newOrder);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Deletes a task (hard delete).</summary>
    public void DeleteTask(int taskId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM \"Tasks\" WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", taskId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Gets the In Tray column for a project.</summary>
    public KanbanColumn? GetInTrayColumn(int projectId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, \"Order\", IsSystem, ProjectId FROM \"Columns\" WHERE \"Order\" = 0 AND ProjectId = @pid LIMIT 1";
        cmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadColumn(reader);
        return null;
    }

    /// <summary>Gets the Done column for a project.</summary>
    public KanbanColumn? GetDoneColumn(int projectId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, \"Order\", IsSystem, ProjectId FROM \"Columns\" WHERE \"Order\" = 999 AND ProjectId = @pid LIMIT 1";
        cmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadColumn(reader);
        return null;
    }

    // ============================
    // Deleted Tasks column
    // ============================

    /// <summary>Gets or creates the Deleted Tasks column for a project.</summary>
    public KanbanColumn GetOrCreateDeletedColumn(int projectId)
    {
        using var checkCmd = _connection.CreateCommand();
        checkCmd.CommandText = "SELECT Id, Name, \"Order\", IsSystem, ProjectId FROM \"Columns\" WHERE \"Order\" = 1000 AND ProjectId = @pid LIMIT 1";
        checkCmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = checkCmd.ExecuteReader();
        if (reader.Read())
            return ReadColumn(reader);
        reader.Close();

        using var createCmd = _connection.CreateCommand();
        createCmd.CommandText = "INSERT INTO \"Columns\" (\"Name\", \"Order\", \"IsSystem\", \"ProjectId\") VALUES ('Deleted Tasks', 1000, 1, @pid); SELECT last_insert_rowid();";
        createCmd.Parameters.AddWithValue("@pid", projectId);
        var id = Convert.ToInt32(createCmd.ExecuteScalar());
        return new KanbanColumn { Id = id, Name = "Deleted Tasks", Order = 1000, IsSystem = true, ProjectId = projectId };
    }

    /// <summary>Moves a task to the Deleted Tasks column.</summary>
    public void MoveTaskToDeletedColumn(int taskId, int projectId)
    {
        var deletedCol = GetOrCreateDeletedColumn(projectId);
        using var countCmd = _connection.CreateCommand();
        countCmd.CommandText = "SELECT COALESCE(MAX(\"Order\"), 0) + 1 FROM \"Tasks\" WHERE ColumnId = @colId";
        countCmd.Parameters.AddWithValue("@colId", deletedCol.Id);
        var nextOrder = Convert.ToInt32(countCmd.ExecuteScalar());

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE \"Tasks\" SET ColumnId = @colId, \"Order\" = @order, IsComplete = 0 WHERE Id = @id";
        cmd.Parameters.AddWithValue("@colId", deletedCol.Id);
        cmd.Parameters.AddWithValue("@order", nextOrder);
        cmd.Parameters.AddWithValue("@id", taskId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Gets the count of tasks in the Deleted Tasks column.</summary>
    public int GetDeletedTaskCount(int projectId)
    {
        var deletedCol = GetOrCreateDeletedColumn(projectId);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM \"Tasks\" WHERE ColumnId = @colId";
        cmd.Parameters.AddWithValue("@colId", deletedCol.Id);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Permanently deletes all tasks in the Deleted Tasks column.</summary>
    public void EmptyDeletedColumn(int projectId)
    {
        var deletedCol = GetOrCreateDeletedColumn(projectId);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM \"Tasks\" WHERE ColumnId = @colId";
        cmd.Parameters.AddWithValue("@colId", deletedCol.Id);
        cmd.ExecuteNonQuery();
    }

    // ============================
    // TaskTypeNames CRUD
    // ============================

    /// <summary>Gets all custom task type names.</summary>
    public Dictionary<TaskType, string> GetTaskTypeNames()
    {
        var names = new Dictionary<TaskType, string>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Type, CustomName FROM \"TaskTypeNames\"";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (Enum.TryParse<TaskType>(reader.GetString(0), out var type))
                names[type] = reader.GetString(1);
        }
        return names;
    }

    /// <summary>Saves a custom name for a task type.</summary>
    public void SaveTaskTypeName(TaskType type, string customName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO \"TaskTypeNames\" (Type, CustomName) VALUES (@type, @name)";
        cmd.Parameters.AddWithValue("@type", type.ToString());
        cmd.Parameters.AddWithValue("@name", customName);
        cmd.ExecuteNonQuery();
    }

    // ============================
    // Comment CRUD
    // ============================

    /// <summary>Gets all comments for a task ordered by creation time.</summary>
    public List<Comment> GetComments(int taskId)
    {
        var comments = new List<Comment>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Text, CreatedAt, TaskId FROM \"Comments\" WHERE TaskId = @tid ORDER BY CreatedAt";
        cmd.Parameters.AddWithValue("@tid", taskId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            comments.Add(new Comment
            {
                Id = reader.GetInt32(0),
                Text = reader.GetString(1),
                CreatedAt = DateTime.Parse(reader.GetString(2)),
                TaskId = reader.GetInt32(3)
            });
        }
        return comments;
    }

    /// <summary>Adds a comment to the database.</summary>
    public void AddComment(Comment comment)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "Comments" (Text, CreatedAt, TaskId) VALUES (@text, @created, @taskId);
            SELECT last_insert_rowid();
        """;
        cmd.Parameters.AddWithValue("@text", comment.Text);
        cmd.Parameters.AddWithValue("@created", comment.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@taskId", comment.TaskId);
        var result = cmd.ExecuteScalar();
        comment.Id = Convert.ToInt32(result);
    }

    /// <summary>Converts a SqliteDataReader row to a KanbanTask model.</summary>
    private static KanbanTask ReadTask(SqliteDataReader reader)
    {
        return new KanbanTask
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            CreatedAt = DateTime.Parse(reader.GetString(2)),
            DueDate = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
            Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
            Type = Enum.Parse<TaskType>(reader.GetString(5)),
            ColumnId = reader.GetInt32(6),
            IsComplete = reader.GetBoolean(7),
            Order = reader.GetInt32(8),
            EstimatedDays = reader.GetInt32(9)
        };
    }

    /// <summary>Converts a SqliteDataReader row to a KanbanColumn model.</summary>
    private static KanbanColumn ReadColumn(SqliteDataReader reader)
    {
        return new KanbanColumn
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Order = reader.GetInt32(2),
            IsSystem = reader.GetBoolean(3),
            ProjectId = reader.GetInt32(4)
        };
    }

    /// <summary>
    /// Closes and disposes the database connection.
    /// </summary>
    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    // ============================
    // Settings CRUD
    // ============================

    /// <summary>Saves a setting (key-value pair) to the database.</summary>
    public void SaveSetting(string key, string value)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO \"Settings\" (Key, Value) VALUES (@key, @value)";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Gets a setting value by key.</summary>
    public string? GetSetting(string key, string? defaultValue = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM \"Settings\" WHERE Key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        var result = cmd.ExecuteScalar();
        return result != null ? result.ToString() : defaultValue;
    }

    /// <summary>Gets all settings as a dictionary.</summary>
    public Dictionary<string, string> GetAllSettings()
    {
        var dict = new Dictionary<string, string>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Key, Value FROM \"Settings\"";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            dict[reader.GetString(0)] = reader.GetString(1);
        return dict;
    }
}