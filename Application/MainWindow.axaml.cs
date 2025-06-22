using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace Application;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly Timer _healthCheckTimer;
    private Panel? _serverStatus;
    private TextBlock? _statusIndicator;
    private Button? _loginButton;

    public MainWindow()
    {
        InitializeComponent();

        _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8063") };
        _healthCheckTimer = new Timer(10000); // 10 seconds
        _healthCheckTimer.Elapsed += async (s, e) => await CheckServerHealth();

        this.Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, EventArgs e)
    {
        _serverStatus = this.FindControl<Panel>("ServerStatus");
        _statusIndicator = _serverStatus?.Children[0] as TextBlock;
        _loginButton = this.FindControl<Button>("LoginButton");

        _healthCheckTimer.Start();
        CheckServerHealth().ConfigureAwait(false);
    }

    private async Task CheckServerHealth()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            bool isHealthy = response.IsSuccessStatusCode;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_statusIndicator != null)
                {
                    _statusIndicator.Text = isHealthy ? "✔" : "x";
                    _statusIndicator.Foreground = new SolidColorBrush(Color.Parse(isHealthy ? "#26a269" : "#c01c28"));
                }

                if (_loginButton != null)
                {
                    _loginButton.IsEnabled = isHealthy;
                }

                if (_serverStatus != null)
                {
                    ToolTip.SetTip(_serverStatus, isHealthy ? "Server Online" : "Server Offline");
                }
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_statusIndicator != null)
                {
                    _statusIndicator.Text = "x";
                    _statusIndicator.Foreground = new SolidColorBrush(Color.Parse("#c01c28"));
                }

                if (_loginButton != null)
                {
                    _loginButton.IsEnabled = false;
                }

                if (_serverStatus != null)
                {
                    ToolTip.SetTip(_serverStatus, "Server Offline");
                }
            });
        }
    }
}