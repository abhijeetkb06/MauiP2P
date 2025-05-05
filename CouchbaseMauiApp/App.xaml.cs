using CouchbaseMauiApp.Core.ViewModels;
using CouchbaseMauiApp.Core.Services;

namespace CouchbaseMauiApp;

public partial class App : Application
{
	public App(MainViewModel mainViewModel)
	{
		InitializeComponent();

		// Register routes
		Routing.RegisterRoute("MainPage", typeof(MainPage));

		// Set initial page
		MainPage = new AppShell();
	}
}