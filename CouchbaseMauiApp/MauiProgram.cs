using CouchbaseMauiApp.Core.Services;
using CouchbaseMauiApp.Core.ViewModels;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace CouchbaseMauiApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register services
		// builder.Services.AddSingleton<DatabaseService>();
		// builder.Services.AddSingleton(typeof(Lazy<>), typeof(Lazy<>));
		builder.Services.AddSingleton<SyncService>();

		// Register view models
		builder.Services.AddSingleton<MainViewModel>();
		// builder.Services.AddSingleton<SplashViewModel>();

		// Register views
		builder.Services.AddSingleton<MainPage>();
		// builder.Services.AddSingleton<SplashPage>();

		return builder.Build();
	}
}
