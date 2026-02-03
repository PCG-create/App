using System.Windows;

namespace CoachPadWpf;

public partial class App : Application
{
    private TrayService? _trayService;
    private readonly LoggingService _logger = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            _logger.LogError(args.Exception.ToString());
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        };
        _trayService = new TrayService();
        _trayService.Initialize();

        var window = new MainWindow();
        window.Hide();
        _trayService.ShowRequested += (_, _) =>
        {
            window.Show();
            window.Activate();
        };
        _trayService.ExitRequested += (_, _) =>
        {
            window.Close();
            Shutdown();
        };
        window.Closed += (_, _) => _trayService.Dispose();
        window.InitializeDetector();
    }
}
