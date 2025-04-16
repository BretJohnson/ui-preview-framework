using DefaultTemplateWithContent.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DefaultTemplateWithContent.Data;
/// <summary>
/// Repository class for managing tasks in the database.
/// </summary>
public class TaskRepository : Repository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRepository"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public TaskRepository(DatabaseManager databaseManager, ILogger<TaskRepository> logger) : base(databaseManager, logger)
    {
    }

    public override async Task CreateTableAsync(SqliteConnection connection)
    {
        try
        {
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Task (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL,
                ProjectID INTEGER NOT NULL
            );";

            await createTableCmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating Task table");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a list of all tasks from the database.
    /// </summary>
    /// <returns>A list of <see cref="ProjectTask"/> objects.</returns>
    public async Task<List<ProjectTask>> ListAsync()
    {
        await using var connection = await OpenConnectionAsync();

        var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM Task";
        var tasks = new List<ProjectTask>();

        await using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(new ProjectTask
            {
                ID = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsCompleted = reader.GetBoolean(2),
                ProjectID = reader.GetInt32(3)
            });
        }

        return tasks;
    }

    /// <summary>
    /// Retrieves a list of tasks associated with a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A list of <see cref="ProjectTask"/> objects.</returns>
    public async Task<List<ProjectTask>> ListAsync(int projectId)
    {
        await using var connection = await OpenConnectionAsync();

        var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM Task WHERE ProjectID = @projectId";
        selectCmd.Parameters.AddWithValue("@projectId", projectId);
        var tasks = new List<ProjectTask>();

        await using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(new ProjectTask
            {
                ID = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsCompleted = reader.GetBoolean(2),
                ProjectID = reader.GetInt32(3)
            });
        }

        return tasks;
    }

    /// <summary>
    /// Retrieves a specific task by its ID.
    /// </summary>
    /// <param name="id">The ID of the task.</param>
    /// <returns>A <see cref="ProjectTask"/> object if found; otherwise, null.</returns>
    public async Task<ProjectTask?> GetAsync(int id)
    {
        await using var connection = await OpenConnectionAsync();

        var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM Task WHERE ID = @id";
        selectCmd.Parameters.AddWithValue("@id", id);

        await using var reader = await selectCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProjectTask
            {
                ID = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsCompleted = reader.GetBoolean(2),
                ProjectID = reader.GetInt32(3)
            };
        }

        return null;
    }

    /// <summary>
    /// Saves a task to the database. If the task ID is 0, a new task is created; otherwise, the existing task is updated.
    /// </summary>
    /// <param name="item">The task to save.</param>
    /// <returns>The ID of the saved task.</returns>
    public async Task<int> SaveItemAsync(ProjectTask item)
    {
        await using var connection = await OpenConnectionAsync();

        var saveCmd = connection.CreateCommand();
        if (item.ID == 0)
        {
            saveCmd.CommandText = @"
            INSERT INTO Task (Title, IsCompleted, ProjectID) VALUES (@title, @isCompleted, @projectId);
            SELECT last_insert_rowid();";
        }
        else
        {
            saveCmd.CommandText = @"
            UPDATE Task SET Title = @title, IsCompleted = @isCompleted, ProjectID = @projectId WHERE ID = @id";
            saveCmd.Parameters.AddWithValue("@id", item.ID);
        }

        saveCmd.Parameters.AddWithValue("@title", item.Title);
        saveCmd.Parameters.AddWithValue("@isCompleted", item.IsCompleted);
        saveCmd.Parameters.AddWithValue("@projectId", item.ProjectID);

        var result = await saveCmd.ExecuteScalarAsync();
        if (item.ID == 0)
        {
            item.ID = Convert.ToInt32(result);
        }

        return item.ID;
    }

    /// <summary>
    /// Deletes a task from the database.
    /// </summary>
    /// <param name="item">The task to delete.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> DeleteItemAsync(ProjectTask item)
    {
        await using var connection = await OpenConnectionAsync();

        var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM Task WHERE ID = @id";
        deleteCmd.Parameters.AddWithValue("@id", item.ID);

        return await deleteCmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops the Task table from the database.
    /// </summary>
    public override async Task DropTableAsync(SqliteConnection connection)
    {
        var dropTableCmd = connection.CreateCommand();
        dropTableCmd.CommandText = "DROP TABLE IF EXISTS Task";
        await dropTableCmd.ExecuteNonQueryAsync();
    }
}
