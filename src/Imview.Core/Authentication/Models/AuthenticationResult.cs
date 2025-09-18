using System;

namespace Imview.Core.Authentication.Models;

public class AuthenticationResult {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public AuthenticationFailureReason FailureReason { get; set; } = AuthenticationFailureReason.None;
    public string? UserId { get; set; }
    public string? SessionKey { get; set; }
    public bool IsPayingUser { get; set; }
    public string? SupportId { get; set; }
    public Exception? Exception { get; set; }

    public static AuthenticationResult CreateSuccess(string userId, string sessionKey, bool isPayingUser = true, string? supportId = null) {
        return new AuthenticationResult {
            Success = true,
            UserId = userId,
            SessionKey = sessionKey,
            IsPayingUser = isPayingUser,
            SupportId = supportId,
            FailureReason = AuthenticationFailureReason.None
        };
    }

    public static AuthenticationResult CreateFailure(AuthenticationFailureReason reason, string errorMessage, Exception? exception = null) {
        return new AuthenticationResult {
            Success = false,
            ErrorMessage = errorMessage,
            FailureReason = reason,
            Exception = exception
        };
    }
}

public enum AuthenticationFailureReason {
    None,
    InvalidCredentials,
    AccountBanned,
    MachineBanned,
    RevisionMismatch,
    ServerError,
    ConnectionLost,
    Timeout,
    UnknownError
}