using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace tododeploy;

public sealed class TodoRepository
{
    private readonly string _connectionString;

    public TodoRepository()
    {
        var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TodoDeploy");
        Directory.CreateDirectory(basePath);
        DbPath = Path.Combine(basePath, "todo.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();
    }

    public string DbPath { get; }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS todo_lists (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS todo_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    list_id INTEGER NOT NULL,
    title TEXT NOT NULL,
    is_done INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(list_id) REFERENCES todo_lists(id) ON DELETE CASCADE
);
";

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<TodoListViewModel>> GetListsAsync()
    {
        var results = new Dictionary<long, TodoListViewModel>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
SELECT l.id, l.title, i.id, i.title, i.is_done
FROM todo_lists l
LEFT JOIN todo_items i ON i.list_id = l.id
ORDER BY l.created_at DESC, i.created_at ASC;
";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var listId = reader.GetInt64(0);
            if (!results.TryGetValue(listId, out var list))
            {
                list = new TodoListViewModel
                {
                    Id = listId,
                    Title = reader.GetString(1)
                };
                results.Add(listId, list);
            }

            if (!reader.IsDBNull(2))
            {
                list.Items.Add(new TodoItemViewModel
                {
                    Id = reader.GetInt64(2),
                    ListId = listId,
                    Title = reader.GetString(3),
                    IsDone = reader.GetInt64(4) == 1
                });
            }
        }

        foreach (var list in results.Values)
        {
            list.RaiseItemSummaryChanged();
        }

        return new List<TodoListViewModel>(results.Values);
    }

    public async Task<long> InsertListAsync(string title)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO todo_lists(title) VALUES ($title);
SELECT last_insert_rowid();
";
        command.Parameters.AddWithValue("$title", title);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task UpdateListTitleAsync(long id, string title)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE todo_lists SET title = $title WHERE id = $id;";
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteListAsync(long id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var deleteItems = connection.CreateCommand();
        deleteItems.CommandText = "DELETE FROM todo_items WHERE list_id = $id;";
        deleteItems.Parameters.AddWithValue("$id", id);
        await deleteItems.ExecuteNonQueryAsync();

        var deleteList = connection.CreateCommand();
        deleteList.CommandText = "DELETE FROM todo_lists WHERE id = $id;";
        deleteList.Parameters.AddWithValue("$id", id);
        await deleteList.ExecuteNonQueryAsync();
    }

    public async Task<long> InsertItemAsync(long listId, string title)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO todo_items(list_id, title, is_done) VALUES ($listId, $title, 0);
SELECT last_insert_rowid();
";
        command.Parameters.AddWithValue("$listId", listId);
        command.Parameters.AddWithValue("$title", title);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task UpdateItemDoneAsync(long itemId, bool isDone)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE todo_items SET is_done = $isDone WHERE id = $id;";
        command.Parameters.AddWithValue("$isDone", isDone ? 1 : 0);
        command.Parameters.AddWithValue("$id", itemId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteItemAsync(long itemId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM todo_items WHERE id = $id;";
        command.Parameters.AddWithValue("$id", itemId);
        await command.ExecuteNonQueryAsync();
    }
}
