using Android.App;
using Android.Content.PM;
using Android.OS;
using Couchbase.Lite.Support;
using Couchbase.Lite.Logging;

namespace CouchbaseMauiApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Couchbase.Lite.Support.Droid.Activate(this);
        LogSinks.Console = new ConsoleLogSink(LogLevel.Verbose);
    }
}
