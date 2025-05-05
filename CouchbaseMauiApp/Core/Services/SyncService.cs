using Couchbase.Lite;
using Couchbase.Lite.Sync;
using Couchbase.Lite.P2P;
using CouchbaseMauiApp.Core.Models;
using System.Text.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;

namespace CouchbaseMauiApp.Core.Services;

public class SyncService : IDisposable
{
    private DatabaseService? _databaseService;
    private URLEndpointListener? _listener;
    private Replicator? _replicator;
    public event Action? ReplicationCompleted;

    // Call this before using SyncService
    public void SetDatabaseService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task StartBroadcastAsync(ConfigModel config)
    {
        if (_databaseService == null) throw new InvalidOperationException("DatabaseService not set");
        try
        {
            // Clear staging and copy selected config to staging
            await _databaseService.ClearStagingAsync();
            await _databaseService.CopyConfigToStagingAsync(config);

            var listenerConfig = new URLEndpointListenerConfiguration(new List<Collection> { _databaseService.StagingCollection })
            {
                DisableTLS = true, // Only for testing
                Authenticator = new ListenerPasswordAuthenticator((sender, username, password) =>
                {
                    // Basic password check - replace with your secure authentication
                    return username == "admin" && SecureStringToString(password) == "password";
                })
            };

            _listener = new URLEndpointListener(listenerConfig);
            await Task.Run(() => _listener.Start());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to start broadcast: {ex.Message}", ex);
        }
    }

    private static string? SecureStringToString(SecureString value)
    {
        if (value == null)
            return null;

        IntPtr bstr = Marshal.SecureStringToBSTR(value);
        try
        {
            return Marshal.PtrToStringBSTR(bstr);
        }
        finally
        {
            Marshal.FreeBSTR(bstr);
        }
    }

    public async Task StartReceiveAsync(string ipAddress, int port)
    {
        if (_databaseService == null) throw new InvalidOperationException("DatabaseService not set");
        try
        {
            var targetUrl = new Uri($"ws://{ipAddress}:{port}/syncflow_db");
            var endpoint = new URLEndpoint(targetUrl);
            var config = new ReplicatorConfiguration(endpoint);
            config.AddCollection(_databaseService.StagingCollection, new CollectionConfiguration());
            config.ReplicatorType = ReplicatorType.PushAndPull;
            config.Continuous = false;
            config.Authenticator = new BasicAuthenticator("admin", "password");

            _replicator = new Replicator(config);
            _replicator.AddChangeListener(async (sender, args) =>
            {
                var status = args.Status;
                Console.WriteLine($"[Replicator] Activity: {status.Activity}, Error: {status.Error}");
                if (status.Activity == ReplicatorActivityLevel.Idle)
                {
                    Console.WriteLine("[Replicator] Replication is idle (likely finished)");
                    await _databaseService.CopyStagingToReceivedAsync();
                    ReplicationCompleted?.Invoke();
                }
                if (status.Activity == ReplicatorActivityLevel.Stopped)
                {
                    Console.WriteLine("[Replicator] Replication stopped");
                    await _databaseService.CopyStagingToReceivedAsync();
                    ReplicationCompleted?.Invoke();
                }
                if (status.Error != null)
                    Console.WriteLine($"[Replicator] Error: {status.Error}");
            });
            await Task.Run(() => _replicator.Start());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to start receive: {ex.Message}", ex);
        }
    }

    public void StopBroadcast()
    {
        _listener?.Stop();
        _listener = null;
    }

    public void StopReceive()
    {
        _replicator?.Stop();
        _replicator = null;
    }

    public string GetBroadcastUrl()
    {
        if (_listener == null)
        {
            throw new InvalidOperationException("Broadcast not started");
        }

        return $"ws://{GetLocalIpAddress()}:{_listener.Port}";
    }

    private string GetLocalIpAddress()
    {
        // This is a simplified version. In production, you should implement proper IP address detection
        return "192.168.1.1"; // Replace with actual IP detection logic
    }

    public void Dispose()
    {
        StopBroadcast();
        StopReceive();
    }
} 