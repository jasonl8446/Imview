using System;

namespace Imview.Core.Authentication.Models;

public class ServerConnectionInfo {
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DisplayAddress => $"{Host}:{Port}";
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
    public string? ErrorMessage { get; set; }
    public DateTime? LastConnected { get; set; }
    public TimeSpan? LastPing { get; set; }

    public static ServerConnectionInfo Parse(string address) {
        if (string.IsNullOrWhiteSpace(address)) {
            throw new ArgumentException("Server address cannot be empty", nameof(address));
        }

        var parts = address.Split(':');
        
        if (parts.Length == 1) {
            // Just hostname, use default port 12000
            return new ServerConnectionInfo {
                Host = parts[0].Trim(),
                Port = 12000
            };
        } else if (parts.Length == 2) {
            // hostname:port format
            if (!int.TryParse(parts[1], out var port) || port <= 0 || port > 65535) {
                throw new FormatException("Port must be a valid number between 1 and 65535");
            }

            return new ServerConnectionInfo {
                Host = parts[0].Trim(),
                Port = port
            };
        } else {
            throw new FormatException("Server address must be in format 'host' or 'host:port'");
        }
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Host) && Port > 0 && Port <= 65535;
}

public enum ConnectionStatus {
    Disconnected,
    Connecting,
    Connected,
    Authenticating,
    Authenticated,
    Failed,
    Timeout
}