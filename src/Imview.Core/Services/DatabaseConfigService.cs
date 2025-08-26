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
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Imview.Core.Common;
using Imview.Core.Database;
using Imview.Core.Views;

namespace Imview.Core.Services;

/// <summary>
/// Service for managing database configuration and connectivity checks
/// </summary>
public static class DatabaseConfigService {

    /// <summary>
    /// Checks if the database is properly configured and connected.
    /// If not, shows the configuration dialog to the user.
    /// </summary>
    /// <param name="parentWindow">Parent window for the configuration dialog</param>
    /// <returns>True if database is configured and connected, false otherwise</returns>
    public static async Task<bool> EnsureDatabaseConfiguredAsync(Window? parentWindow) {
        try {
            // First check if we have basic configuration
            if (!IsDatabaseConfigured()) {
                return await ShowConfigurationDialogAsync(parentWindow);
            }

            // Test the connection
            if (!await TestDatabaseConnectionAsync()) {
                return await ShowConfigurationDialogAsync(parentWindow);
            }

            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error checking database configuration: {ex.Message}");
            return await ShowConfigurationDialogAsync(parentWindow);
        }
    }

    /// <summary>
    /// Checks if the database is configured (URL and database name are provided)
    /// </summary>
    public static bool IsDatabaseConfigured() {
        try {
            var dbUrl = ConfigurationManager.Settings["Database.WorldDatabaseUrl"].AsString();
            var dbName = ConfigurationManager.Settings["Database.WorldDatabaseName"].AsString();

            return !string.IsNullOrWhiteSpace(dbUrl) && !string.IsNullOrWhiteSpace(dbName);
        }
        catch {
            return false;
        }
    }

    /// <summary>
    /// Tests the database connection
    /// </summary>
    public static async Task<bool> TestDatabaseConnectionAsync() {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                return false;
            }

            using var session = store.OpenAsyncSession();
            // Simple connection test - just try to get database statistics
            var stats = store.Maintenance.Send(new Raven.Client.Documents.Operations.GetStatisticsOperation());
            
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shows the database configuration dialog
    /// </summary>
    private static async Task<bool> ShowConfigurationDialogAsync(Window? parentWindow) {
        try {
            var configDialog = new DatabaseConfigWindow();
            
            bool? result;
            if (parentWindow != null) {
                result = await configDialog.ShowDialog<bool?>(parentWindow);
            } else {
                // If no parent window, show as a regular window and return false
                // This case should be rare in practice
                configDialog.Show();
                result = false;
            }

            return result == true && IsDatabaseConfigured();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error showing configuration dialog: {ex.Message}");
            return false;
        }
    }
}