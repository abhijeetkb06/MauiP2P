using Couchbase.Lite;
using Couchbase.Lite.Query;
using CouchbaseMauiApp.Core.Models;
using System.Text.Json;

namespace CouchbaseMauiApp.Core.Services;

public class DatabaseService : IDisposable
{
    private readonly Database _database;
    private readonly Collection _collection;
    private const string DatabaseName = "syncflow_db";

    public DatabaseService()
    {
        var directory = Path.Combine(FileSystem.AppDataDirectory, "databases");
        Directory.CreateDirectory(directory);
        
        var config = new DatabaseConfiguration
        {
            Directory = directory
        };

        _database = new Database(DatabaseName, config);
        _collection = _database.GetDefaultCollection();
    }

    public Database Database => _database;
    public Collection DefaultCollection => _collection;

    public async Task SaveConfigAsync(ConfigModel config)
    {
        var doc = new MutableDocument(config.Id);
        doc.SetString("type", "config");
        doc.SetString("deviceName", config.DeviceName);
        doc.SetString("configData", JsonSerializer.Serialize(config.ConfigData));
        doc.SetString("createdAt", config.CreatedAt.ToString("O"));
        doc.SetString("lastModified", config.LastModified.ToString("O"));
        await Task.Run(() => _collection.Save(doc));
    }

    public async Task<List<ConfigModel>> GetAllConfigsAsync()
    {
        var query = QueryBuilder.Select(
                SelectResult.Expression(Meta.ID),
                SelectResult.Property("deviceName"),
                SelectResult.Property("configData"),
                SelectResult.Property("createdAt"),
                SelectResult.Property("lastModified")
            )
            .From(DataSource.Collection(_collection))
            .Where(Expression.Property("type").EqualTo(Expression.String("config")));

        var results = await Task.Run(() => query.Execute());
        var configs = new List<ConfigModel>();

        foreach (var result in results)
        {
            var id = result.GetString("id");
            var deviceName = result.GetString("deviceName");
            var configData = result.GetString("configData");
            var createdAt = DateTime.Parse(result.GetString("createdAt"));
            var lastModified = DateTime.Parse(result.GetString("lastModified"));

            if (configData != null)
            {
                var config = new ConfigModel
                {
                    Id = id,
                    DeviceName = deviceName,
                    ConfigData = JsonSerializer.Deserialize<Dictionary<string, object>>(configData),
                    CreatedAt = createdAt,
                    LastModified = lastModified
                };
                configs.Add(config);
            }
        }

        return configs;
    }

    public async Task DeleteConfigAsync(string id)
    {
        var doc = _collection.GetDocument(id);
        if (doc != null)
        {
            await Task.Run(() => _collection.Delete(doc));
        }
    }

    public Database GetStagingDatabase()
    {
        return _database;
    }

    public void Dispose()
    {
        _database?.Dispose();
    }
} 