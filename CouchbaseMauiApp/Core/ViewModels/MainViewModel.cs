using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CouchbaseMauiApp.Core.Models;
using CouchbaseMauiApp.Core.Services;
using System.Collections.ObjectModel;

namespace CouchbaseMauiApp.Core.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private DatabaseService? _databaseService;
    private readonly SyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<ConfigModel> _configs;

    [ObservableProperty]
    private bool _isBroadcasting;

    [ObservableProperty]
    private bool _isReceiving;

    [ObservableProperty]
    private string _broadcastUrl = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private int _port;

    public MainViewModel(SyncService syncService)
    {
        _syncService = syncService;
        _configs = new ObservableCollection<ConfigModel>();
        // Do not initialize DatabaseService here
    }

    private void EnsureDatabaseService()
    {
        if (_databaseService == null)
        {
            _databaseService = new DatabaseService();
            _syncService.SetDatabaseService(_databaseService);
        }
    }

    private async void LoadConfigsAsync()
    {
        try
        {
            EnsureDatabaseService();
            await EnsureMockConfigsAsync();
            var configs = await _databaseService.GetAllConfigsAsync();
            Configs.Clear();
            foreach (var config in configs)
            {
                Configs.Add(config);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load configs: {ex.Message}";
        }
    }

    private async Task EnsureMockConfigsAsync()
    {
        var configs = await _databaseService.GetAllConfigsAsync();
        if (configs.Count == 0)
        {
            var now = DateTime.UtcNow;
            var mock1 = new ConfigModel
            {
                Id = Guid.NewGuid().ToString(),
                DeviceName = "Demo Config 1",
                ConfigData = new Dictionary<string, object> { { "settingA", true }, { "settingB", 42 } },
                CreatedAt = now,
                LastModified = now
            };
            var mock2 = new ConfigModel
            {
                Id = Guid.NewGuid().ToString(),
                DeviceName = "Demo Config 2",
                ConfigData = new Dictionary<string, object> { { "settingA", false }, { "settingB", 99 } },
                CreatedAt = now,
                LastModified = now
            };
            var mock3 = new ConfigModel
            {
                Id = Guid.NewGuid().ToString(),
                DeviceName = "Demo Config 3",
                ConfigData = new Dictionary<string, object> { { "settingA", true }, { "settingB", 7 }, { "settingC", "extra" } },
                CreatedAt = now,
                LastModified = now
            };
            await _databaseService.SaveConfigAsync(mock1);
            await _databaseService.SaveConfigAsync(mock2);
            await _databaseService.SaveConfigAsync(mock3);
        }
    }

    [RelayCommand]
    private async Task StartBroadcast(ConfigModel config)
    {
        try
        {
            IsBroadcasting = true;
            await _syncService.StartBroadcastAsync(config);
            BroadcastUrl = _syncService.GetBroadcastUrl();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start broadcast: {ex.Message}";
            IsBroadcasting = false;
        }
    }

    [RelayCommand]
    private async Task StartReceive()
    {
        try
        {
            IsReceiving = true;
            await _syncService.StartReceiveAsync(IpAddress, Port);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start receive: {ex.Message}";
            IsReceiving = false;
        }
    }

    [RelayCommand]
    private void StopBroadcast()
    {
        _syncService.StopBroadcast();
        IsBroadcasting = false;
        BroadcastUrl = string.Empty;
    }

    [RelayCommand]
    private void StopReceive()
    {
        _syncService.StopReceive();
        IsReceiving = false;
    }

    [RelayCommand]
    private async Task DeleteConfig(ConfigModel config)
    {
        try
        {
            EnsureDatabaseService();
            await _databaseService.DeleteConfigAsync(config.Id);
            Configs.Remove(config);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete config: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveConfig(ConfigModel config)
    {
        try
        {
            EnsureDatabaseService();
            await _databaseService.SaveConfigAsync(config);
            LoadConfigsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save config: {ex.Message}";
        }
    }
} 