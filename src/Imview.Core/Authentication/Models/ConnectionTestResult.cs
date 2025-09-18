using System;

namespace Imview.Core.Authentication.Models;

public class ConnectionTestResult {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public ConnectionFailureReason FailureReason { get; set; } = ConnectionFailureReason.None;
    public Exception? Exception { get; set; }

    public static ConnectionTestResult CreateSuccess(TimeSpan responseTime) {
        return new ConnectionTestResult {
            Success = true,
            ResponseTime = responseTime,
            FailureReason = ConnectionFailureReason.None
        };
    }

    public static ConnectionTestResult CreateFailure(ConnectionFailureReason reason, string errorMessage, Exception? exception = null) {
        return new ConnectionTestResult {
            Success = false,
            ErrorMessage = errorMessage,
            FailureReason = reason,
            Exception = exception
        };
    }
}

public enum ConnectionFailureReason {
    None,
    InvalidAddress,
    DnsResolutionFailed,
    ConnectionTimeout,
    ConnectionRefused,
    NetworkUnreachable,
    ProtocolError,
    UnknownError
}