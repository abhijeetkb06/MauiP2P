using Couchbase.Lite;
using Couchbase.Lite.Query;
using CouchbaseMauiApp.Core.Models;
using System.Text.Json;

namespace CouchbaseMauiApp.Core.Services;

public class DatabaseService : IDisposable
{
    private readonly Database _database;
    private readonly Collection _localCollection;
    private readonly Collection _receivedCollection;
    private readonly Collection _stagingCollection;
    private const string DatabaseName = "syncflow_db";
    private const string LocalCollectionName = "local_configs";
    private const string ReceivedCollectionName = "received_configs";
    private const string StagingCollectionName = "staging_configs";

    public DatabaseService()
    {
        var directory = Path.Combine(FileSystem.AppDataDirectory, "databases");
        Directory.CreateDirectory(directory);
        
        var config = new DatabaseConfiguration
        {
            Directory = directory
        };

        _database = new Database(DatabaseName, config);
        _localCollection = _database.CreateCollection(LocalCollectionName);
        _receivedCollection = _database.CreateCollection(ReceivedCollectionName);
        _stagingCollection = _database.CreateCollection(StagingCollectionName);
    }

    public Database Database => _database;
    public Collection LocalCollection => _localCollection;
    public Collection ReceivedCollection => _receivedCollection;
    public Collection StagingCollection => _stagingCollection;

    public async Task SaveConfigAsync(ConfigModel config, bool isReceived = false)
    {
        var collection = isReceived ? _receivedCollection : _localCollection;
        var doc = new MutableDocument(config.Id);
        doc.SetString("type", "config");
        doc.SetString("deviceName", config.DeviceName);
        doc.SetString("configData", JsonSerializer.Serialize(config.ConfigData));
        doc.SetString("createdAt", config.CreatedAt.ToString("O"));
        doc.SetString("lastModified", config.LastModified.ToString("O"));
        await Task.Run(() => collection.Save(doc));
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
            .From(DataSource.Collection(_localCollection))
            .Where(Expression.Property("type").EqualTo(Expression.String("config")));

        var results = await Task.Run(() => query.Execute());
        var configs = new List<ConfigModel>();

        foreach (var result in results)
        {
            var id = result.GetString("id");
            var deviceName = result.GetString("deviceName");
            var configData = result.GetString("configData");
            var createdAtStr = result.GetString("createdAt");
            var lastModifiedStr = result.GetString("lastModified");

            if (id != null && deviceName != null && configData != null && 
                createdAtStr != null && lastModifiedStr != null)
            {
                var createdAt = DateTime.Parse(createdAtStr);
                var lastModified = DateTime.Parse(lastModifiedStr);
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

    public async Task<List<ConfigModel>> GetReceivedConfigsAsync()
    {
        var query = QueryBuilder.Select(
                SelectResult.Expression(Meta.ID),
                SelectResult.Property("deviceName"),
                SelectResult.Property("configData"),
                SelectResult.Property("createdAt"),
                SelectResult.Property("lastModified")
            )
            .From(DataSource.Collection(_receivedCollection))
            .Where(Expression.Property("type").EqualTo(Expression.String("config")));

        var results = await Task.Run(() => query.Execute());
        var configs = new List<ConfigModel>();

        foreach (var result in results)
        {
            var id = result.GetString("id");
            var deviceName = result.GetString("deviceName");
            var configData = result.GetString("configData");
            var createdAtStr = result.GetString("createdAt");
            var lastModifiedStr = result.GetString("lastModified");

            if (id != null && deviceName != null && configData != null && 
                createdAtStr != null && lastModifiedStr != null)
            {
                var createdAt = DateTime.Parse(createdAtStr);
                var lastModified = DateTime.Parse(lastModifiedStr);
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

    public async Task DeleteConfigAsync(string id, bool isReceived = false)
    {
        var collection = isReceived ? _receivedCollection : _localCollection;
        var doc = collection.GetDocument(id);
        if (doc != null)
        {
            await Task.Run(() => collection.Delete(doc));
        }
    }

    public Database GetStagingDatabase()
    {
        return _database;
    }

    public async Task CopyConfigToStagingAsync(ConfigModel config)
    {
        var doc = new MutableDocument(config.Id);
        doc.SetString("type", "config");
        doc.SetString("deviceName", config.DeviceName);
        doc.SetString("configData", JsonSerializer.Serialize(config.ConfigData));
        doc.SetString("createdAt", config.CreatedAt.ToString("O"));
        doc.SetString("lastModified", config.LastModified.ToString("O"));
        await Task.Run(() => _stagingCollection.Save(doc));
    }

    public async Task ClearStagingAsync()
    {
        var query = QueryBuilder.Select(SelectResult.Expression(Meta.ID))
            .From(DataSource.Collection(_stagingCollection));
        var results = await Task.Run(() => query.Execute());
        foreach (var result in results)
        {
            var id = result.GetString("id");
            if (id != null)
            {
                var doc = _stagingCollection.GetDocument(id);
                if (doc != null)
                    _stagingCollection.Delete(doc);
            }
        }
    }

    public async Task CopyStagingToReceivedAsync()
    {
        var query = QueryBuilder.Select(
                SelectResult.Expression(Meta.ID),
                SelectResult.Property("deviceName"),
                SelectResult.Property("configData"),
                SelectResult.Property("createdAt"),
                SelectResult.Property("lastModified")
            )
            .From(DataSource.Collection(_stagingCollection))
            .Where(Expression.Property("type").EqualTo(Expression.String("config")));

        var results = await Task.Run(() => query.Execute());
        foreach (var result in results)
        {
            var id = result.GetString("id");
            var deviceName = result.GetString("deviceName");
            var configData = result.GetString("configData");
            var createdAtStr = result.GetString("createdAt");
            var lastModifiedStr = result.GetString("lastModified");
            if (id != null && deviceName != null && configData != null && createdAtStr != null && lastModifiedStr != null)
            {
                var createdAt = DateTime.Parse(createdAtStr);
                var lastModified = DateTime.Parse(lastModifiedStr);
                var config = new ConfigModel
                {
                    Id = id,
                    DeviceName = deviceName,
                    ConfigData = JsonSerializer.Deserialize<Dictionary<string, object>>(configData),
                    CreatedAt = createdAt,
                    LastModified = lastModified
                };
                var doc = new MutableDocument(config.Id);
                doc.SetString("type", "config");
                doc.SetString("deviceName", config.DeviceName);
                doc.SetString("configData", JsonSerializer.Serialize(config.ConfigData));
                doc.SetString("createdAt", config.CreatedAt.ToString("O"));
                doc.SetString("lastModified", config.LastModified.ToString("O"));
                _receivedCollection.Save(doc);
            }
        }
    }

    public void Dispose()
    {
        _database?.Dispose();
    }
} 