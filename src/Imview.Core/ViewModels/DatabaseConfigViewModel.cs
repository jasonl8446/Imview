/*
BSD 3-Clause License

Copyright (c) 2024, Jooty

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.
*/

using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ReactiveUI;
using Imview.Core.Common;
using Imview.Core.Database;
using Raven.Client.Documents.Linq;

namespace Imview.Core.ViewModels;

public class DatabaseConfigViewModel : ViewModelBase {
    
    private readonly Window _parentWindow;
    private string _databaseUrl = "";
    private string _databaseName = "WorldDB";
    private string _certificatePath = "";
    private bool _isTestingConnection = false;
    private string _connectionStatus = "";
    private IBrush _connectionStatusColor = Brushes.Gray;
    private bool _canSave = false;

    public DatabaseConfigViewModel(Window parentWindow) {
        _parentWindow = parentWindow;
        
        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        BrowseCertificateCommand = ReactiveCommand.CreateFromTask(BrowseCertificateAsync);
        SaveCommand = ReactiveCommand.Create(Save);
        CancelCommand = ReactiveCommand.Create(Cancel);

        // Load existing configuration if available
        LoadExistingConfiguration();
        
        // Set up property change handling
        this.WhenAnyValue(x => x.DatabaseUrl, x => x.DatabaseName)
            .Subscribe(_ => UpdateCanSave());
    }


    public string DatabaseUrl {
        get => _databaseUrl;
        set {
            this.RaiseAndSetIfChanged(ref _databaseUrl, value);
            UpdateCanSave();
        }
    }

    public string DatabaseName {
        get => _databaseName;
        set {
            this.RaiseAndSetIfChanged(ref _databaseName, value);
            UpdateCanSave();
        }
    }

    public string CertificatePath {
        get => _certificatePath;
        set => this.RaiseAndSetIfChanged(ref _certificatePath, value);
    }

    public bool IsTestingConnection {
        get => _isTestingConnection;
        set => this.RaiseAndSetIfChanged(ref _isTestingConnection, value);
    }

    public string ConnectionStatus {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    public IBrush ConnectionStatusColor {
        get => _connectionStatusColor;
        set => this.RaiseAndSetIfChanged(ref _connectionStatusColor, value);
    }

    public bool CanSave {
        get => _canSave;
        set => this.RaiseAndSetIfChanged(ref _canSave, value);
    }

    public string TestButtonText => IsTestingConnection ? "Testing..." : "Test Connection";

    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseCertificateCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    private void LoadExistingConfiguration() {
        try {
            var dbUrl = ConfigurationManager.Settings["Database.WorldDatabaseUrl"].AsString();
            var dbName = ConfigurationManager.Settings["Database.WorldDatabaseName"].AsString("WorldDB");
            var certPath = ConfigurationManager.Settings["Database.WorldDatabaseCertificatePath"].AsString();

            DatabaseUrl = dbUrl;
            DatabaseName = dbName;
            CertificatePath = certPath;
        }
        catch {
            // Use defaults if configuration loading fails
        }
    }

    private void UpdateCanSave() {
        CanSave = !string.IsNullOrWhiteSpace(DatabaseUrl) && 
                 !string.IsNullOrWhiteSpace(DatabaseName);
    }

    private async Task TestConnectionAsync() {
        IsTestingConnection = true;
        ConnectionStatus = "Testing connection...";
        ConnectionStatusColor = Brushes.Orange;

        try {
            // Temporarily save configuration for testing
            var originalUrl = ConfigurationManager.Settings["Database.WorldDatabaseUrl"].AsString();
            var originalName = ConfigurationManager.Settings["Database.WorldDatabaseName"].AsString();
            var originalCert = ConfigurationManager.Settings["Database.WorldDatabaseCertificatePath"].AsString();

            // Set test configuration
            ConfigurationManager.SetSetting("Database.WorldDatabaseUrl", DatabaseUrl);
            ConfigurationManager.SetSetting("Database.WorldDatabaseName", DatabaseName);
            ConfigurationManager.SetSetting("Database.WorldDatabaseCertificatePath", CertificatePath);

            // Test the connection
            var store = WorldDatabase.Instance.Store;
            if (store != null) {
                using var session = store.OpenAsyncSession();
                // Simple connection test - just try to open a session
                var stats = store.Maintenance.Send(new Raven.Client.Documents.Operations.GetStatisticsOperation());
                
                ConnectionStatus = "Connection successful!";
                ConnectionStatusColor = Brushes.Green;
            } else {
                ConnectionStatus = "Failed to create database connection.";
                ConnectionStatusColor = Brushes.Red;
            }

            // Restore original configuration
            ConfigurationManager.SetSetting("Database.WorldDatabaseUrl", originalUrl ?? "");
            ConfigurationManager.SetSetting("Database.WorldDatabaseName", originalName ?? "WorldDB");
            ConfigurationManager.SetSetting("Database.WorldDatabaseCertificatePath", originalCert ?? "");
        }
        catch (Exception ex) {
            ConnectionStatus = $"Connection failed: {ex.Message}";
            ConnectionStatusColor = Brushes.Red;
        }
        finally {
            IsTestingConnection = false;
        }
    }

    private async Task BrowseCertificateAsync() {
        var options = new FilePickerOpenOptions {
            Title = "Select Certificate File",
            AllowMultiple = false,
            FileTypeFilter = [
                new("Certificate Files") {
                    Patterns = ["*.pfx", "*.p12", "*.cer", "*.crt"]
                }
            ]
        };

        var result = await _parentWindow.StorageProvider.OpenFilePickerAsync(options);
        if (result.Count > 0) {
            CertificatePath = result[0].Path.LocalPath;
        }
    }

    private void Save() {
        try {
            // Save configuration
            ConfigurationManager.SetSetting("Database.WorldDatabaseUrl", DatabaseUrl);
            ConfigurationManager.SetSetting("Database.WorldDatabaseName", DatabaseName);
            ConfigurationManager.SetSetting("Database.WorldDatabaseCertificatePath", CertificatePath);
            ConfigurationManager.SetSetting("Application.FirstRun", "False");

            // Close the window
            _parentWindow.Close(true);
        }
        catch (Exception ex) {
            ConnectionStatus = $"Error saving configuration: {ex.Message}";
            ConnectionStatusColor = Brushes.Red;
        }
    }

    private void Cancel() {
        _parentWindow.Close(false);
    }
}