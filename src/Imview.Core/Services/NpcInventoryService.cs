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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imview.Core.Database;
using Imview.Core.Models;
using Raven.Client.Documents;

namespace Imview.Core.Services;

/// <summary>
/// Service for querying NPC inventory data from the worlddata database
/// </summary>
public static class NpcInventoryService
{
    /// <summary>
    /// Queries the database for inventory data for a specific NPC template ID
    /// </summary>
    /// <param name="templateId">The template ID of the NPC to find inventory for</param>
    /// <returns>NpcInventory if found, null otherwise</returns>
    public static async Task<NpcInventory?> GetNpcInventoryAsync(ulong templateId)
    {
        const string CollectionName = "NpcInventory";
        
        try
        {
            var store = WorldDatabase.Instance.Store;
            if (store == null)
            {
                return null;
            }

            using var session = store.OpenAsyncSession();
            
            // Query for the NpcInventory by template ID in NpcInventory collection
            var npcInventory = await session
                .Query<NpcInventory>(collectionName: CollectionName)
                .Where(ni => ni.TemplateID == templateId)
                .FirstOrDefaultAsync();

            return npcInventory;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error querying NPC inventory data: {ex.Message}");
            Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Checks if inventory data exists for a specific NPC template ID
    /// </summary>
    /// <param name="templateId">The template ID of the NPC to check for</param>
    /// <returns>True if inventory data exists, false otherwise</returns>
    public static async Task<bool> HasNpcInventoryAsync(ulong templateId)
    {
        try
        {
            var inventoryData = await GetNpcInventoryAsync(templateId);
            return inventoryData != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking NPC inventory existence: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves NPC inventory data to the database
    /// </summary>
    /// <param name="npcInventory">The NPC inventory data to save</param>
    /// <returns>True if saved successfully, false otherwise</returns>
    public static async Task<bool> SaveNpcInventoryAsync(NpcInventory npcInventory)
    {
        const string CollectionName = "NpcInventory";
        
        try
        {
            var store = WorldDatabase.Instance.Store;
            if (store == null)
            {
                throw new Exception("Database connection not available.");
            }
            
            using var session = store.OpenAsyncSession();
            
            // Query for existing inventory data
            var existingInventory = await session
                .Query<NpcInventory>(collectionName: CollectionName)
                .Where(ni => ni.TemplateID == npcInventory.TemplateID)
                .FirstOrDefaultAsync();
            
            if (existingInventory == null)
            {
                // Create new inventory entry
                await session.StoreAsync(npcInventory);
                
                // Set collection metadata
                var metadata = session.Advanced.GetMetadataFor(npcInventory);
                metadata[Raven.Client.Constants.Documents.Metadata.Collection] = CollectionName;
            }
            else
            {
                // Update existing inventory
                existingInventory.Inventory = npcInventory.Inventory;
            }
            
            await session.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving NPC inventory: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Gets all NPC template IDs that have inventories in the database
    /// </summary>
    /// <returns>HashSet of template IDs that have inventory data</returns>
    public static async Task<HashSet<ulong>> GetNpcTemplateIdsWithInventoriesAsync()
    {
        const string CollectionName = "NpcInventory";
        
        try
        {
            var store = WorldDatabase.Instance.Store;
            if (store == null)
            {
                Console.WriteLine("World database is not available");
                return new HashSet<ulong>();
            }

            using var session = store.OpenAsyncSession();
            
            var templateIds = await session
                .Query<NpcInventory>(collectionName: CollectionName)
                .Select(ni => ni.TemplateID)
                .ToListAsync();

            var result = new HashSet<ulong>(templateIds);
            Console.WriteLine($"Found {result.Count} NPC template IDs with inventory data");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error querying NPC template IDs with inventories: {ex.Message}");
            return new HashSet<ulong>();
        }
    }

    /// <summary>
    /// Gets all NPC inventory documents (for debugging/testing purposes)
    /// </summary>
    /// <returns>Array of all NpcInventory objects</returns>
    public static async Task<NpcInventory[]> GetAllNpcInventoriesAsync()
    {
        const string CollectionName = "NpcInventory";
        
        try
        {
            var store = WorldDatabase.Instance.Store;
            if (store == null)
            {
                Console.WriteLine("World database is not available");
                return Array.Empty<NpcInventory>();
            }

            using var session = store.OpenAsyncSession();
            
            var results = await session
                .Query<NpcInventory>(collectionName: CollectionName)
                .ToArrayAsync();

            Console.WriteLine($"Found {results.Length} NpcInventory documents in NpcInventory collection");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error querying all NPC inventories: {ex.Message}");
            return Array.Empty<NpcInventory>();
        }
    }
}
