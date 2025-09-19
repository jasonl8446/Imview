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
using System.Threading.Tasks;
using Imview.Core.Database;
using Imview.Core.Models;
using Raven.Client.Documents;
using Imcodec.ObjectProperty.TypeCache;

namespace Imview.Core.Services;

/// <summary>
/// Service for querying ZoneTransfer data from the worlddata database
/// </summary>
public static class ZoneTransferService
{
    /// <summary>
    /// Queries the database for teleport data for a specific zone and trigger
    /// </summary>
    /// <param name="zoneName">Name of the zone to search in</param>
    /// <param name="triggerName">Name of the trigger to find teleport data for</param>
    /// <returns>ResTeleport if found, null otherwise</returns>
    public static async Task<ResTeleport?> GetTeleportDataAsync(string zoneName, string triggerName)
    {
        const string CollectionName = "ZoneTransfer";
        
        try
        {
            var store = WorldDatabase.Instance.Store;
            if (store == null)
            {
                return null;
            }

            using var session = store.OpenAsyncSession();
            
            // Query for the WizardZoneData by zone name in ZoneTransfer collection
            var zoneData = await session
                .Query<WizardZoneData>(collectionName: CollectionName)
                .Where(zd => zd.ZoneName == zoneName)
                .FirstOrDefaultAsync();

            if (zoneData == null)
                return null;

            // Find the teleport data for the specific trigger
            var teleportData = zoneData.Teleports
                ?.FirstOrDefault(t => t.TriggerName == triggerName);

            return teleportData?.Teleport;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error querying teleport data: {ex.Message}");
            Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Checks if teleport data exists for a specific zone and trigger
    /// </summary>
    /// <param name="zoneName">Name of the zone to search in</param>
    /// <param name="triggerName">Name of the trigger to check for</param>
    /// <returns>True if teleport data exists, false otherwise</returns>
    public static async Task<bool> HasTeleportDataAsync(string zoneName, string triggerName)
    {
        try
        {
            var teleportData = await GetTeleportDataAsync(zoneName, triggerName);
            return teleportData != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking teleport data existence: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all zone transfer documents (for debugging/testing purposes)
    /// </summary>
    /// <returns>Array of all WizardZoneData objects</returns>
    public static async Task<WizardZoneData[]> GetAllZoneTransfersAsync()
    {
        const string CollectionName = "ZoneTransfer";
        
        try
        {
            var store = WorldDatabase.Instance.Store;
            if (store == null)
            {
                Console.WriteLine("World database is not available");
                return Array.Empty<WizardZoneData>();
            }

            using var session = store.OpenAsyncSession();
            
            var results = await session
                .Query<WizardZoneData>(collectionName: CollectionName)
                .ToArrayAsync();

            Console.WriteLine($"Found {results.Length} WizardZoneData documents in ZoneTransfer collection");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error querying all zone transfers: {ex.Message}");
            return Array.Empty<WizardZoneData>();
        }
    }
}
