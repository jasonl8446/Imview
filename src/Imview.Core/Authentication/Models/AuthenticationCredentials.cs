using System;

namespace Imview.Core.Authentication.Models;

public class AuthenticationCredentials {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string ClientRevision { get; set; } = "r778979_Wizard_1_580_0_Live";
    public string ClientVersion { get; set; } = "1.580.0.Live";
    public string DataRevision { get; set; } = "r778979";
    public string Locale { get; set; } = "en_US";

    public bool IsValid => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    public AuthenticationCredentials() {
        // Generate a default machine ID if not provided
        MachineId = GenerateDefaultMachineId();
    }

    private static string GenerateDefaultMachineId() {
        // Generate a consistent machine ID based on system info
        var machineInfo = $"{Environment.MachineName}-{Environment.UserName}-{Environment.OSVersion}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(machineInfo));
        
        // Convert to a format similar to what Imlight expects (GID format)
        var guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);
        return new Guid(guidBytes).ToString("N");
    }
}