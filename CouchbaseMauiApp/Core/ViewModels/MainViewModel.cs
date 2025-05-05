using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CouchbaseMauiApp.Core.Models;
using CouchbaseMauiApp.Core.Services;
using System.Collections.ObjectModel;
using System.Threading;
using Couchbase.Lite;

namespace CouchbaseMauiApp.Core.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private DatabaseService? _databaseService;
    private readonly SyncService _syncService;
    private ListenerToken? _dbChangeListenerToken;

    [ObservableProperty]
    private ObservableCollection<ConfigModel> _configs;

    [ObservableProperty]
    private ConfigModel? _selectedConfig;

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

    [ObservableProperty]
    private bool _showReceivedOnly = false;

    [ObservableProperty]
    private ObservableCollection<ConfigModel> _receivedConfigs = new();

    public MainViewModel(SyncService syncService)
    {
        _syncService = syncService;
        _configs = new ObservableCollection<ConfigModel>();
        // Do not initialize DatabaseService here

        // Subscribe to replication complete event to reload configs
        _syncService.ReplicationCompleted += () =>
        {
            // Reload configs on the main thread
            MainThread.BeginInvokeOnMainThread(() => LoadConfigsAsync());
        };
    }

    private void EnsureDatabaseService()
    {
        if (_databaseService == null)
        {
            _databaseService = new DatabaseService();
            _syncService.SetDatabaseService(_databaseService);

            // Add database change listener for real-time UI updates
            _dbChangeListenerToken = _databaseService.LocalCollection.AddChangeListener((sender, args) =>
            {
                // Reload configs on the main thread when database changes
                MainThread.BeginInvokeOnMainThread(() => LoadConfigsAsync());
            });
        }
    }

    public void Dispose()
    {
        _dbChangeListenerToken?.Remove();
        _dbChangeListenerToken = null;
    }

    private async void LoadConfigsAsync()
    {
        try
        {
            EnsureDatabaseService();
            if (_databaseService == null) return;
            
            await EnsureMockConfigsAsync();
            // Local configs
            var configs = await _databaseService.GetAllConfigsAsync();
            Configs.Clear();
            foreach (var config in configs)
            {
                Configs.Add(config);
            }
            // Received configs
            var received = await _databaseService.GetReceivedConfigsAsync();
            ReceivedConfigs.Clear();
            foreach (var config in received)
            {
                ReceivedConfigs.Add(config);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load configs: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        ShowReceivedOnly = !ShowReceivedOnly;
        LoadConfigsAsync();
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
            await _databaseService.SaveConfigAsync(mock1, false);
            await _databaseService.SaveConfigAsync(mock2, false);
            await _databaseService.SaveConfigAsync(mock3, false);
        }
    }

    [RelayCommand]
    private async Task StartBroadcast()
    {
        try
        {
            if (SelectedConfig == null)
            {
                ErrorMessage = "Please select a config to broadcast.";
                return;
            }
            IsBroadcasting = true;
            await _syncService.StartBroadcastAsync(SelectedConfig);
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
            // Delete from the appropriate collection based on current view mode
            await _databaseService.DeleteConfigAsync(config.Id, ShowReceivedOnly);
            if (ShowReceivedOnly)
                ReceivedConfigs.Remove(config);
            else
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
            // Always save to local collection when manually saving
            await _databaseService.SaveConfigAsync(config, false);
            LoadConfigsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save config: {ex.Message}";
        }
    }
} 