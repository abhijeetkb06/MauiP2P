using CouchbaseMauiApp.Core.Models;
using CommunityToolkit.Maui.Views;

namespace CouchbaseMauiApp.Views;

public partial class EditConfigPopup : Popup
{
    public ConfigModel Config { get; private set; }
    public event EventHandler<ConfigModel>? SaveClicked;
    public event EventHandler? CancelClicked;

    public EditConfigPopup(ConfigModel config)
    {
        InitializeComponent();
        // Deep copy to avoid editing the original until save
        Config = new ConfigModel
        {
            Id = config.Id,
            DeviceName = config.DeviceName,
            ConfigData = new Dictionary<string, object>(config.ConfigData),
            CreatedAt = config.CreatedAt,
            LastModified = config.LastModified
        };
        // Ensure FormattedConfigData is in sync
        Config.FormattedConfigData = new System.Collections.ObjectModel.ObservableCollection<ConfigModel.ConfigDataItem>(
            Config.ConfigData.Select(kvp => new ConfigModel.ConfigDataItem { Key = kvp.Key, Value = kvp.Value })
        );
        BindingContext = Config;
        SaveButton.Clicked += (s, e) => {
            Config.SyncFormattedToConfigData();
            SaveClicked?.Invoke(this, Config);
            Close();
        };
        CancelButton.Clicked += (s, e) => { CancelClicked?.Invoke(this, EventArgs.Empty); Close(); };
    }
} 