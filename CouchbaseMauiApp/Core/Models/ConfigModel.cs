using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace CouchbaseMauiApp.Core.Models;

public partial class ConfigModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("configData")]
    public Dictionary<string, object> ConfigData { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public IEnumerable<KeyValuePair<string, object>> ConfigDataPairs => ConfigData;

    public partial class ConfigDataItem : ObservableObject
    {
        [ObservableProperty]
        private string key = string.Empty;
        [ObservableProperty]
        private object value = string.Empty;
    }

    public ObservableCollection<ConfigDataItem> FormattedConfigData { get; set; }

    public ConfigModel()
    {
        FormattedConfigData = new ObservableCollection<ConfigDataItem>(
            ConfigData.Select(kvp => new ConfigDataItem { Key = kvp.Key, Value = kvp.Value })
        );
    }

    public void SyncFormattedToConfigData()
    {
        ConfigData = FormattedConfigData.ToDictionary(item => item.Key, item => item.Value);
    }

    public void SyncConfigDataToFormatted()
    {
        FormattedConfigData = new ObservableCollection<ConfigDataItem>(
            ConfigData.Select(kvp => new ConfigDataItem { Key = kvp.Key, Value = kvp.Value })
        );
    }
} 