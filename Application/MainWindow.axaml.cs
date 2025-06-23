using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
    private Button? _registerButton;
    private TextBlock? _registerMessage;
    private Grid? _loginForm;
    private Grid? _registerForm;
    private TabControl? _formTabs;

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
        _registerButton = this.FindControl<Button>("RegisterButton");
        _registerMessage = this.FindControl<TextBlock>("RegisterMessage");
        _loginForm = this.FindControl<Grid>("LoginForm");
        _registerForm = this.FindControl<Grid>("RegisterForm");
        _formTabs = this.FindControl<TabControl>("FormTabs");

        // Add event handlers for form tabs
        if (_formTabs != null)
        {
            _formTabs.SelectionChanged += FormTabs_SelectionChanged;
        }

        // Add event handlers for register button
        if (_registerButton != null)
        {
            _registerButton.Click += RegisterButton_Click;
        }

        _healthCheckTimer.Start();
        CheckServerHealth().ConfigureAwait(false);
    }

    private void FormTabs_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_formTabs?.SelectedIndex == 0) // Login tab
        {
            // Slide login form in, register form out
            if (_loginForm != null) _loginForm.Margin = new Thickness(0, 0, 0, 0);
            if (_registerForm != null) _registerForm.Margin = new Thickness(400, 0, -400, 0);
        }
        else // Register tab
        {
            // Slide login form out, register form in
            if (_loginForm != null) _loginForm.Margin = new Thickness(-400, 0, 400, 0);
            if (_registerForm != null) _registerForm.Margin = new Thickness(0, 0, 0, 0);
        }
    }

    private async void RegisterButton_Click(object? sender, RoutedEventArgs e)
    {
        var usernameBox = this.FindControl<TextBox>("RegisterUsernameBox");
        var passwordBox = this.FindControl<TextBox>("RegisterPasswordBox");
        var verifyBox = this.FindControl<TextBox>("VerifyPasswordBox");

        string username = usernameBox?.Text?.Trim() ?? string.Empty;
        string password = passwordBox?.Text ?? string.Empty;
        string verify = verifyBox?.Text ?? string.Empty;

        // Reset message
        if (_registerMessage != null)
        {
            _registerMessage.Text = string.Empty;
        }

        // Validate inputs
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowRegistrationMessage("Username and password are required", false);
            return;
        }

        if (password != verify)
        {
            ShowRegistrationMessage("Passwords do not match", false);
            return;
        }

        // Disable button during registration
        if (_registerButton != null)
        {
            _registerButton.IsEnabled = false;
        }

        try
        {
            ShowRegistrationMessage("Registering...", true);

            var registrationData = new
            {
                username,
                password
            };

            var json = JsonSerializer.Serialize(registrationData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                ShowRegistrationMessage("Registration successful! You can now log in.", true);

                // Clear form fields
                if (usernameBox != null) usernameBox.Text = string.Empty;
                if (passwordBox != null) passwordBox.Text = string.Empty;
                if (verifyBox != null) verifyBox.Text = string.Empty;

                // After a short delay, switch to login tab
                await Task.Delay(1500);
                if (_formTabs != null)
                {
                    _formTabs.SelectedIndex = 0; // Switch to login tab
                }
            }
            else
            {
                var responseText = await response.Content.ReadAsStringAsync();
                ShowRegistrationMessage($"Registration failed: {responseText}", false);
            }
        }
        catch (Exception ex)
        {
            ShowRegistrationMessage($"Error: {ex.Message}", false);
        }
        finally
        {
            // Re-enable button after registration attempt
            if (_registerButton != null)
            {
                _registerButton.IsEnabled = true;
            }
        }
    }

    private void ShowRegistrationMessage(string message, bool isSuccess)
    {
        if (_registerMessage != null)
        {
            _registerMessage.Text = message;
            _registerMessage.Foreground = new SolidColorBrush(Color.Parse(isSuccess ? "#26a269" : "#c01c28"));
        }
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

                if (_registerButton != null)
                {
                    _registerButton.IsEnabled = isHealthy;
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

                if (_registerButton != null)
                {
                    _registerButton.IsEnabled = false;
                }

                if (_serverStatus != null)
                {
                    ToolTip.SetTip(_serverStatus, "Server Offline");
                }
            });
        }
    }
}