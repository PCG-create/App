using Microsoft.UI.Xaml;

namespace CoachPadWin;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnConnect(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.ConnectAsync();
    }

    private void OnDisconnect(object sender, RoutedEventArgs e)
    {
        ViewModel.Disconnect();
    }

    private void OnStart(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.StartCoachingAsync();
    }

    private void OnStop(object sender, RoutedEventArgs e)
    {
        ViewModel.StopCoaching();
    }

    private void OnEndCall(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.EndCallAsync();
    }
}
