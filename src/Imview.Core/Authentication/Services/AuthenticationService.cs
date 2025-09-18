using System;
using System.Threading.Tasks;
using Imview.Core.Authentication.Models;
using Imview.Core.Authentication.Networking;
using Imview.Core.Authentication.Configuration;

namespace Imview.Core.Authentication.Services;

public class AuthenticationService {
    private readonly ServerConnectionService _connectionService;
    private ImvightTcpClient? _client;

    public event EventHandler<string>? StatusChanged;

    public AuthenticationService() {
        _connectionService = new ServerConnectionService();
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        ServerConnectionInfo serverInfo, 
        AuthenticationCredentials credentials) {
        
        try {
            UpdateStatus("Connecting to server...");
            
            // First validate the connection
            var connectionResult = await _connectionService.ValidateImvightServerAsync(serverInfo);
            if (!connectionResult.Success) {
                return AuthenticationResult.CreateFailure(
                    AuthenticationFailureReason.ConnectionLost,
                    connectionResult.ErrorMessage ?? "Failed to connect to server"
                );
            }

            UpdateStatus("Establishing session...");
            
            // For now, we'll implement basic authentication without the full Imlight protocol
            // In a complete implementation, this would include the full session handshake
            // and user authentication using MSG_USER_AUTHEN_V3
            
            // Simulate authentication delay
            await Task.Delay(1000);

            UpdateStatus("Authenticating user...");
            
            // TODO: Implement actual user authentication using Imlight protocol
            // This would involve:
            // 1. Creating MSG_USER_AUTHEN_V3 message with encrypted credentials
            // 2. Sending it to the server
            // 3. Waiting for MSG_USER_AUTHEN_RSP
            // 4. Processing the response
            
            // For now, simulate authentication based on basic validation
            if (!credentials.IsValid) {
                return AuthenticationResult.CreateFailure(
                    AuthenticationFailureReason.InvalidCredentials,
                    "Username and password are required"
                );
            }

            // Save successful server connection
            ServerConfigManager.SaveLastUsedServer(serverInfo.DisplayAddress);
            
            UpdateStatus("Authentication successful!");
            
            // Return success with dummy data for now
            return AuthenticationResult.CreateSuccess(
                userId: "dummy-user-id",
                sessionKey: "dummy-session-key",
                isPayingUser: true
            );
        }
        catch (Exception ex) {
            return AuthenticationResult.CreateFailure(
                AuthenticationFailureReason.UnknownError,
                $"Authentication failed: {ex.Message}",
                ex
            );
        }
    }

    public void Disconnect() {
        _client?.Dispose();
        _client = null;
        UpdateStatus("Disconnected");
    }

    private void UpdateStatus(string status) {
        StatusChanged?.Invoke(this, status);
    }

    public void Dispose() {
        Disconnect();
    }
}