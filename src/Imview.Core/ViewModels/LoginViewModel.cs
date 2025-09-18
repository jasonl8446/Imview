using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using Imview.Core.Authentication.Models;
using Imview.Core.Authentication.Services;
using Imview.Core.Authentication.Configuration;

namespace Imview.Core.ViewModels;

public class LoginViewModel : ViewModelBase, IDisposable {
    private readonly AuthenticationService _authService;
    private readonly ServerConnectionService _connectionService;
    
    private string _serverAddress = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberServer = true;
    private bool _isConnecting = false;
    private bool _isTestingConnection = false;
    private string _statusMessage = "Ready to connect...";
    private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
    
    public LoginViewModel() {
        _authService = new AuthenticationService();
        _connectionService = new ServerConnectionService();
        
        // Load saved server configuration
        var config = ServerConfigManager.Load();
        ServerAddress = config.LastUsedServer;
        RememberServer = config.RememberServer;
        
        // Setup commands with proper reactive bindings
        var canLoginObservable = this.WhenAnyValue(
            x => x.IsConnecting,
            x => x.IsTestingConnection,
            x => x.ServerAddress,
            x => x.Username,
            x => x.Password,
            (connecting, testing, server, username, password) =>
                !connecting && !testing && 
                !string.IsNullOrWhiteSpace(server) && 
                !string.IsNullOrWhiteSpace(username) && 
                !string.IsNullOrWhiteSpace(password));

        var canTestConnectionObservable = this.WhenAnyValue(
            x => x.IsConnecting,
            x => x.IsTestingConnection,
            x => x.ServerAddress,
            (connecting, testing, server) =>
                !connecting && !testing && !string.IsNullOrWhiteSpace(server));

        LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync, canLoginObservable);
        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync, canTestConnectionObservable);
        CancelCommand = ReactiveCommand.Create(Cancel);
        
        // Subscribe to authentication service status updates
        _authService.StatusChanged += OnAuthStatusChanged;
        
        // Update status when connection status changes
        this.WhenAnyValue(x => x.ConnectionStatus)
            .Subscribe(_ => UpdateStatusMessage());
    }

    // Properties
    public string ServerAddress {
        get => _serverAddress;
        set => this.RaiseAndSetIfChanged(ref _serverAddress, value);
    }

    public string Username {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    public string Password {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool RememberServer {
        get => _rememberServer;
        set => this.RaiseAndSetIfChanged(ref _rememberServer, value);
    }

    public bool IsConnecting {
        get => _isConnecting;
        set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    public bool IsTestingConnection {
        get => _isTestingConnection;
        set => this.RaiseAndSetIfChanged(ref _isTestingConnection, value);
    }

    public string StatusMessage {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ConnectionStatus ConnectionStatus {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    // Computed properties
    public bool CanLogin => !IsConnecting && !IsTestingConnection && 
                           !string.IsNullOrWhiteSpace(ServerAddress) && 
                           !string.IsNullOrWhiteSpace(Username) && 
                           !string.IsNullOrWhiteSpace(Password);

    public bool CanTestConnection => !IsConnecting && !IsTestingConnection && 
                                   !string.IsNullOrWhiteSpace(ServerAddress);

    public string ConnectionStatusText => ConnectionStatus switch {
        ConnectionStatus.Disconnected => "○ Disconnected",
        ConnectionStatus.Connecting => "● Connecting...",
        ConnectionStatus.Connected => "● Connected", 
        ConnectionStatus.Authenticating => "● Authenticating...",
        ConnectionStatus.Authenticated => "● Authenticated",
        ConnectionStatus.Failed => "● Connection Failed",
        ConnectionStatus.Timeout => "● Connection Timeout",
        _ => "○ Unknown"
    };

    public string ConnectionStatusColor => ConnectionStatus switch {
        ConnectionStatus.Connected => "Green",
        ConnectionStatus.Authenticated => "Blue",
        ConnectionStatus.Connecting => "Orange",
        ConnectionStatus.Authenticating => "Orange",
        ConnectionStatus.Failed => "Red",
        ConnectionStatus.Timeout => "Red",
        _ => "Gray"
    };

    // Commands
    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    // Events
    public event EventHandler<AuthenticationResult>? AuthenticationCompleted;

    private async Task LoginAsync() {
        try {
            IsConnecting = true;
            ConnectionStatus = ConnectionStatus.Connecting;
            
            // Parse server info
            var serverInfo = ServerConnectionInfo.Parse(ServerAddress);
            serverInfo.Status = ConnectionStatus.Connecting;
            
            // Create credentials
            var credentials = new AuthenticationCredentials {
                Username = Username,
                Password = Password
            };
            
            ConnectionStatus = ConnectionStatus.Authenticating;
            
            // Perform authentication
            var result = await _authService.AuthenticateAsync(serverInfo, credentials);
            
            if (result.Success) {
                ConnectionStatus = ConnectionStatus.Authenticated;
                StatusMessage = "Login successful!";
                
                // Save server config if remember is checked
                if (RememberServer) {
                    var config = ServerConfigManager.Load();
                    config.LastUsedServer = ServerAddress;
                    config.RememberServer = RememberServer;
                    ServerConfigManager.Save(config);
                }
                
                // Notify completion
                AuthenticationCompleted?.Invoke(this, result);
            }
            else {
                ConnectionStatus = ConnectionStatus.Failed;
                StatusMessage = result.ErrorMessage ?? "Authentication failed";
            }
        }
        catch (Exception ex) {
            ConnectionStatus = ConnectionStatus.Failed;
            StatusMessage = $"Login error: {ex.Message}";
        }
        finally {
            IsConnecting = false;
        }
    }

    public async Task TestConnectionAsync() {
        try {
            IsTestingConnection = true;
            ConnectionStatus = ConnectionStatus.Connecting;
            StatusMessage = "Testing connection...";
            
            System.Diagnostics.Debug.WriteLine($"Testing connection to: {ServerAddress}");
            
            var result = await _connectionService.TestConnectionAsync(ServerAddress);
            
            System.Diagnostics.Debug.WriteLine($"Connection result: Success={result.Success}, Error={result.ErrorMessage}");
            
            if (result.Success) {
                ConnectionStatus = ConnectionStatus.Connected;
                StatusMessage = $"Connection successful! ({result.ResponseTime?.TotalMilliseconds:F0}ms)";
            }
            else {
                ConnectionStatus = ConnectionStatus.Failed;
                StatusMessage = result.ErrorMessage ?? "Connection test failed";
            }
        }
        catch (Exception ex) {
            ConnectionStatus = ConnectionStatus.Failed;
            StatusMessage = $"Connection test error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Connection test exception: {ex}");
        }
        finally {
            IsTestingConnection = false;
        }
    }

    private void Cancel() {
        _authService.Disconnect();
        IsConnecting = false;
        IsTestingConnection = false;
        ConnectionStatus = ConnectionStatus.Disconnected;
        StatusMessage = "Cancelled";
    }

    private void OnAuthStatusChanged(object? sender, string status) {
        StatusMessage = status;
    }

    private void UpdateStatusMessage() {
        if (!IsConnecting && !IsTestingConnection) {
            StatusMessage = ConnectionStatus switch {
                ConnectionStatus.Disconnected => "Ready to connect...",
                ConnectionStatus.Connected => "Server connection verified",
                ConnectionStatus.Failed => "Connection failed",
                ConnectionStatus.Timeout => "Connection timed out",
                _ => StatusMessage
            };
        }
    }

    public void Dispose() {
        _authService.StatusChanged -= OnAuthStatusChanged;
        _authService.Dispose();
    }
}