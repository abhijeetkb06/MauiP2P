using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CouchbaseMauiApp.Core.ViewModels;

public partial class SplashViewModel : ObservableObject
{
    [RelayCommand]
    private async Task StartAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
} 