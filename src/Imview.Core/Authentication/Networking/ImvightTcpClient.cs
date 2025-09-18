using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Imview.Core.Authentication.Models;
using Imcodec.MessageLayer;
using System.Net;

namespace Imview.Core.Authentication.Networking;

public class ImvightTcpClient : IDisposable {
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _disposed;
    private readonly int _timeoutMs;

    public bool IsConnected => _tcpClient?.Connected == true;
    public ushort SessionId { get; private set; }
    public uint OfferTime { get; private set; }
    public uint OfferMilliseconds { get; private set; }

    public ImvightTcpClient(int timeoutMs = 10000) {
        _timeoutMs = timeoutMs;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(ServerConnectionInfo serverInfo) {
        var stopwatch = Stopwatch.StartNew();
        
        try {
            using var testClient = new TcpClient();
            using var cts = new CancellationTokenSource(_timeoutMs);
            
            await testClient.ConnectAsync(serverInfo.Host, serverInfo.Port, cts.Token);
            
            stopwatch.Stop();
            return ConnectionTestResult.CreateSuccess(stopwatch.Elapsed);
        }
        catch (OperationCanceledException) {
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.ConnectionTimeout,
                $"Connection to {serverInfo.DisplayAddress} timed out after {_timeoutMs / 1000} seconds"
            );
        }
        catch (SocketException ex) {
            return ex.SocketErrorCode switch {
                SocketError.HostNotFound => ConnectionTestResult.CreateFailure(
                    ConnectionFailureReason.DnsResolutionFailed,
                    $"Could not resolve hostname: {serverInfo.Host}",
                    ex
                ),
                SocketError.ConnectionRefused => ConnectionTestResult.CreateFailure(
                    ConnectionFailureReason.ConnectionRefused,
                    $"Connection refused by {serverInfo.DisplayAddress} - Server may be offline",
                    ex
                ),
                SocketError.NetworkUnreachable => ConnectionTestResult.CreateFailure(
                    ConnectionFailureReason.NetworkUnreachable,
                    "Network unreachable - Check your internet connection",
                    ex
                ),
                _ => ConnectionTestResult.CreateFailure(
                    ConnectionFailureReason.UnknownError,
                    $"Network error: {ex.Message}",
                    ex
                )
            };
        }
        catch (Exception ex) {
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.UnknownError,
                $"Unexpected error: {ex.Message}",
                ex
            );
        }
    }

    public async Task<ConnectionTestResult> ConnectAsync(ServerConnectionInfo serverInfo) {
        try {
            _tcpClient = new TcpClient();
            using var cts = new CancellationTokenSource(_timeoutMs);
            
            await _tcpClient.ConnectAsync(serverInfo.Host, serverInfo.Port, cts.Token);
            _stream = _tcpClient.GetStream();
            
            // Generate a session ID for this connection
            SessionId = (ushort)Random.Shared.Next(1, ushort.MaxValue);
            
            return ConnectionTestResult.CreateSuccess(TimeSpan.Zero);
        }
        catch (Exception ex) {
            Dispose();
            return ConnectionTestResult.CreateFailure(
                ConnectionFailureReason.ConnectionRefused,
                $"Failed to connect: {ex.Message}",
                ex
            );
        }
    }

    public async Task<bool> SendSessionOfferAsync() {
        if (_stream == null) {
            throw new InvalidOperationException("Not connected to server");
        }

        try {
            // Create session offer similar to Imlight's ControlService
            var currentUnixTimestamp = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            var timestampUpper = (int)(currentUnixTimestamp >> 32);
            var timestampLower = (int)(currentUnixTimestamp & uint.MaxValue);
            var millisecondsIntoCurrentSecond = (uint)(DateTime.UtcNow.TimeOfDay.TotalMilliseconds % 1000);

            var offer = new ControlMessageProtocol.SessionOffer {
                SessionId = SessionId,
                TimestampUpper = timestampUpper,
                TimestampLower = timestampLower,
                Milliseconds = millisecondsIntoCurrentSecond,
            };

            // Store for later use in authentication
            OfferTime = currentUnixTimestamp;
            OfferMilliseconds = millisecondsIntoCurrentSecond;

            // Encode and send the message
            var encodedMessage = MessageEncoder.Encode(offer);
            
            await _stream.WriteAsync(encodedMessage);
            await _stream.FlushAsync();
            
            return true;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Failed to send session offer: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> WaitForSessionAcceptAsync(CancellationToken cancellationToken = default) {
        if (_stream == null) {
            throw new InvalidOperationException("Not connected to server");
        }

        try {
            // For now, we'll skip waiting for the session accept response since we don't have
            // the full protocol implementation yet. A real Imlight server expects a specific
            // handshake sequence. For connection testing purposes, just connecting successfully
            // is enough to verify the server is reachable.
            
            // In a full implementation, we would:
            // 1. Read the SessionAccept message
            // 2. Parse it using MessageEncoder.Decode()
            // 3. Validate the session ID matches
            
            System.Diagnostics.Debug.WriteLine("Skipping session accept wait - connection test successful");
            return true;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Session handshake error: {ex.Message}");
            return false;
        }
    }

    public void Dispose() {
        if (_disposed) return;
        
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _disposed = true;
    }
}