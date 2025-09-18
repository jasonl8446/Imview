using System;
using System.IO;
using System.Text.Json;
using Imview.Core.Authentication.Models;
using Imview.Core.Common;

namespace Imview.Core.Authentication.Configuration;

public class ServerConfigManager {
    private const string ConfigFileName = "server-config.json";
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Imview", 
        ConfigFileName
    );

    public class ServerConfig {
        public string LastUsedServer { get; set; } = "qa.r10.one";
        public bool RememberServer { get; set; } = true;
        public int ConnectionTimeoutSeconds { get; set; } = 10;
        public DateTime? LastConnected { get; set; }
    }

    public static ServerConfig Load() {
        try {
            if (File.Exists(ConfigFilePath)) {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<ServerConfig>(json);
                return config ?? new ServerConfig();
            }
        }
        catch (Exception ex) {
            // Log error but continue with defaults
            System.Diagnostics.Debug.WriteLine($"Failed to load server config: {ex.Message}");
        }

        return new ServerConfig();
    }

    public static void Save(ServerConfig config) {
        try {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { 
                WriteIndented = true 
            });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex) {
            // Log error but don't throw - saving config is not critical
            System.Diagnostics.Debug.WriteLine($"Failed to save server config: {ex.Message}");
        }
    }

    public static void SaveLastUsedServer(string serverAddress) {
        var config = Load();
        if (config.RememberServer) {
            config.LastUsedServer = serverAddress;
            config.LastConnected = DateTime.UtcNow;
            Save(config);
        }
    }

    public static ServerConnectionInfo GetDefaultServer() {
        var config = Load();
        try {
            return ServerConnectionInfo.Parse(config.LastUsedServer);
        }
        catch {
            // If parsing fails, return the hardcoded default
            return new ServerConnectionInfo { Host = "qa.r10.one", Port = 12000 };
        }
    }
}