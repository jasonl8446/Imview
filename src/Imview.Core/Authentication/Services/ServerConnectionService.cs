using System;
using System.Threading.Tasks;
using Imview.Core.Authentication.Models;
using Imview.Core.Authentication.Networking;

namespace Imview.Core.Authentication.Services;

public class ServerConnectionService {
    private readonly int _connectionTimeoutMs;

    public ServerConnectionService(int connectionTimeoutMs = 10000) {
        _connectionTimeoutMs = connectionTimeoutMs;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(string serverAddress) {
        try {
            var serverInfo = ServerConnectionInfo.Parse(serverAddress);
            return await TestConnectionAsync(serverInfo);
        }
        catch (FormatException ex) {
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.InvalidAddress,
                $"Invalid server address format: {ex.Message}",
                ex
            );
        }
        catch (ArgumentException ex) {
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.InvalidAddress,
                $"Invalid server address: {ex.Message}",
                ex
            );
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(ServerConnectionInfo serverInfo) {
        if (!serverInfo.IsValid) {
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.InvalidAddress,
                "Invalid server information provided"
            );
        }

        using var client = new ImvightTcpClient(_connectionTimeoutMs);
        return await client.TestConnectionAsync(serverInfo);
    }

    public async Task<ConnectionTestResult> ValidateImvightServerAsync(string serverAddress) {
        try {
            var serverInfo = ServerConnectionInfo.Parse(serverAddress);
            return await ValidateImvightServerAsync(serverInfo);
        }
        catch (Exception ex) {
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.InvalidAddress,
                $"Invalid server address: {ex.Message}",
                ex
            );
        }
    }

    public async Task<ConnectionTestResult> ValidateImvightServerAsync(ServerConnectionInfo serverInfo) {
        using var client = new ImvightTcpClient(_connectionTimeoutMs);
        
        // For now, just test basic TCP connectivity to the server
        // A full implementation would include the complete Imlight protocol handshake
        var connectionResult = await client.ConnectAsync(serverInfo);
        if (!connectionResult.Success) {
            return connectionResult;
        }

        try {
            // Basic connectivity test - if we can connect, the server is reachable
            // In the future, we can add full protocol validation here
            await Task.Delay(100); // Small delay to ensure connection is stable
            
            return ConnectionTestResult.CreateSuccess(TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex) {
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.ProtocolError,
                $"Connection validation failed: {ex.Message}",
                ex
            );
        }
    }

    public static (string host, int port) ParseServerAddress(string address) {
        var serverInfo = ServerConnectionInfo.Parse(address);
        return (serverInfo.Host, serverInfo.Port);
    }
}