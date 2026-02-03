using System.Windows;
using System.Windows.Input;

namespace CoachPadWpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private bool _isCollapsed;
    private readonly System.Windows.Threading.DispatcherTimer _followTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
        _followTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _followTimer.Tick += (_, _) => FollowActiveWindow();
        _followTimer.Start();
    }

    public void InitializeDetector()
    {
        _viewModel.InitializeDetector(() =>
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                Activate();
            });
        });
    }

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        await _viewModel.ConnectAsync();
    }

    private void OnDisconnect(object sender, RoutedEventArgs e)
    {
        _viewModel.Disconnect();
    }

    private async void OnStart(object sender, RoutedEventArgs e)
    {
        await _viewModel.StartCoachingAsync();
    }

    private void OnStop(object sender, RoutedEventArgs e)
    {
        _viewModel.StopCoaching();
    }

    private void OnSendTranscript(object sender, RoutedEventArgs e)
    {
        _viewModel.SendDebugTranscript();
    }

    private async void OnEndCall(object sender, RoutedEventArgs e)
    {
        await _viewModel.EndCallAsync();
    }

    private void OnHide(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnToggleCollapse(object sender, RoutedEventArgs e)
    {
        _isCollapsed = !_isCollapsed;
        Height = _isCollapsed ? 220 : 640;
    }

    private void OnDeleteAll(object sender, RoutedEventArgs e)
    {
        _viewModel.DeleteAllData();
    }

    private void OnPrivacy(object sender, RoutedEventArgs e)
    {
        var window = new PrivacyWindow
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void FollowActiveWindow()
    {
        if (!_viewModel.FollowActiveWindow)
        {
            return;
        }
        if (!WindowEnumerator.TryGetForegroundRect(out var rect))
        {
            return;
        }
        var width = Width;
        var height = Height;
        var left = rect.Right - width - 16;
        var top = rect.Top + 16;
        Left = Math.Max(0, left);
        Top = Math.Max(0, top);
    }
}
