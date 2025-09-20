// Copyright (c) 2025 Jay Kulsh
// Licensed under the BSD 3-Clause License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imview.Core.Database.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Imview.Core.Database.Services
{
    /// <summary>
    /// Service for managing NPC spell inventories in the database
    /// </summary>
    public static class NpcSpellInventoryService
    {
        private const string CollectionName = "NpcSpellInventory";

        /// <summary>
        /// Gets the spell inventory for a specific NPC template ID
        /// </summary>
        /// <param name="templateId">The NPC template ID</param>
        /// <returns>The NPC spell inventory if found, null otherwise</returns>
        public static async Task<NpcSpellInventory?> GetNpcSpellInventoryAsync(ulong templateId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] GetNpcSpellInventoryAsync called for template ID: {templateId}");
                var database = WorldDatabase.Instance.Store;
                if (database == null)
                {
                    Console.WriteLine("[ERROR] Database is not available");
                    return null;
                }

                using var session = database.OpenAsyncSession();
                Console.WriteLine($"[DEBUG] Querying database for NpcSpellInventory with template ID: {templateId}");
                
                // Query the collection directly without relying on CLR type matching
                // Use RQL (Raven Query Language) to query by collection
                var result = await session.Advanced.AsyncRawQuery<dynamic>(
                    "from NpcSpellInventory where TemplateID = $templateId")
                    .AddParameter("templateId", templateId)
                    .FirstOrDefaultAsync();
                
                if (result == null)
                {
                    Console.WriteLine($"[DEBUG] No document found in NpcSpellInventory collection for template ID: {templateId}");
                    return null;
                }
                
                Console.WriteLine($"[DEBUG] Found document in NpcSpellInventory: {result}");
                
                // Convert the dynamic result to our model
                var spellInventory = new NpcSpellInventory((ulong)(long)result.TemplateID);
                
                if (result.Spells != null)
                {
                    foreach (var spell in result.Spells)
                    {
                        var spellItem = new SpellInventoryItem(
                            (ulong)(long)spell.TemplateID,
                            (ulong)(long)spell.RequiredSpellID,
                            (int)spell.Level
                        );
                        spellInventory.Spells.Add(spellItem);
                    }
                }

                if (result == null)
                {
                    Console.WriteLine($"[DEBUG] No spell inventory found for template ID: {templateId}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Found spell inventory with {result.Spells?.Count ?? 0} spells for template ID: {templateId}");
                }
                
                Console.WriteLine($"[DEBUG] Converted to NpcSpellInventory with {spellInventory.Spells.Count} spells");
                return spellInventory;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get NPC spell inventory for template ID {templateId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves or updates an NPC spell inventory in the database
        /// </summary>
        /// <param name="spellInventory">The spell inventory to save</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> SaveNpcSpellInventoryAsync(NpcSpellInventory spellInventory)
        {
            try
            {
                var database = WorldDatabase.Instance.Store;
                if (database == null)
                {
                    Console.WriteLine("[ERROR] Database is not available");
                    return false;
                }

                using var session = database.OpenAsyncSession();
                
                // Simple LINQ query for existing document in the correct collection
                var existing = await session.Query<NpcSpellInventory>(collectionName: CollectionName)
                    .FirstOrDefaultAsync(x => x.TemplateID == spellInventory.TemplateID);

                if (existing != null)
                {
                    // Update existing document properties
                    existing.Spells.Clear();
                    existing.Spells.AddRange(spellInventory.Spells);
                    Console.WriteLine($"[INFO] Updated existing spell inventory for NPC template ID: {spellInventory.TemplateID}");
                }
                else
                {
                    // Store new document in the correct collection
                    await session.StoreAsync(spellInventory, $"{CollectionName}/{spellInventory.TemplateID}");
                    Console.WriteLine($"[INFO] Created new spell inventory for NPC template ID: {spellInventory.TemplateID}");
                }

                await session.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save NPC spell inventory for template ID {spellInventory.TemplateID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an NPC spell inventory from the database
        /// </summary>
        /// <param name="templateId">The NPC template ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> DeleteNpcSpellInventoryAsync(ulong templateId)
        {
            try
            {
                var database = WorldDatabase.Instance.Store;
                if (database == null)
                {
                    Console.WriteLine("[ERROR] Database is not available");
                    return false;
                }

                using var session = database.OpenAsyncSession();
                
                var existing = await session.Advanced.AsyncRawQuery<dynamic>(
                    "from NpcSpellInventory where TemplateID = $templateId")
                    .AddParameter("templateId", templateId)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    var docId = session.Advanced.GetDocumentId(existing);
                    session.Delete(docId);
                    await session.SaveChangesAsync();
                    Console.WriteLine($"[INFO] Deleted spell inventory for NPC template ID: {templateId} (doc ID: {docId})");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[INFO] No spell inventory found to delete for NPC template ID: {templateId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete NPC spell inventory for template ID {templateId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all NPC template IDs that have spell inventories in the database
        /// </summary>
        /// <returns>HashSet of template IDs that have spell inventories</returns>
        public static async Task<HashSet<ulong>> GetNpcTemplateIdsWithSpellInventoriesAsync()
        {
            try
            {
                Console.WriteLine("[DEBUG] GetNpcTemplateIdsWithSpellInventoriesAsync called");
                var database = WorldDatabase.Instance.Store;
                if (database == null)
                {
                    Console.WriteLine("[ERROR] Database is not available");
                    return new HashSet<ulong>();
                }

                using var session = database.OpenAsyncSession();
                Console.WriteLine("[DEBUG] Querying all NpcSpellInventory documents from database");
                
                // Query the collection directly to get all template IDs
                var results = await session.Advanced.AsyncRawQuery<dynamic>(
                    "from NpcSpellInventory select TemplateID")
                    .ToListAsync();
                
                var templateIds = new List<ulong>();
                foreach (var result in results)
                {
                    if (result.TemplateID != null)
                    {
                        templateIds.Add((ulong)(long)result.TemplateID);
                    }
                }

                Console.WriteLine($"[DEBUG] Found {templateIds.Count} template IDs with spell inventories: [{string.Join(", ", templateIds)}]");
                return new HashSet<ulong>(templateIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get NPC template IDs with spell inventories: {ex.Message}");
                return new HashSet<ulong>();
            }
        }

        /// <summary>
        /// Checks if an NPC has a spell inventory in the database
        /// </summary>
        /// <param name="templateId">The NPC template ID</param>
        /// <returns>True if the NPC has a spell inventory, false otherwise</returns>
        public static async Task<bool> HasNpcSpellInventoryAsync(ulong templateId)
        {
            try
            {
                var database = WorldDatabase.Instance.Store;
                if (database == null)
                {
                    Console.WriteLine("[ERROR] Database is not available");
                    return false;
                }

                using var session = database.OpenAsyncSession();
                var count = await session.Advanced.AsyncRawQuery<dynamic>(
                    "from NpcSpellInventory where TemplateID = $templateId")
                    .AddParameter("templateId", templateId)
                    .CountAsync();
                
                var exists = count > 0;

                Console.WriteLine($"[DEBUG] HasNpcSpellInventoryAsync for template ID {templateId}: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to check if NPC has spell inventory for template ID {templateId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all NPC spell inventories from the database
        /// </summary>
        /// <returns>List of all NPC spell inventories</returns>
        public static async Task<List<NpcSpellInventory>> GetAllNpcSpellInventoriesAsync()
        {
            try
            {
                var database = WorldDatabase.Instance.Store;
                if (database == null)
                {
                    Console.WriteLine("[ERROR] Database is not available");
                    return new List<NpcSpellInventory>();
                }

                using var session = database.OpenAsyncSession();
                
                // Query all documents in the NpcSpellInventory collection
                var results = await session.Advanced.AsyncRawQuery<dynamic>(
                    "from NpcSpellInventory")
                    .ToListAsync();
                
                var inventories = new List<NpcSpellInventory>();
                
                foreach (var result in results)
                {
                    var spellInventory = new NpcSpellInventory((ulong)(long)result.TemplateID);
                    
                    if (result.Spells != null)
                    {
                        foreach (var spell in result.Spells)
                        {
                            var spellItem = new SpellInventoryItem(
                                (ulong)(long)spell.TemplateID,
                                (ulong)(long)spell.RequiredSpellID,
                                (int)spell.Level
                            );
                            spellInventory.Spells.Add(spellItem);
                        }
                    }
                    
                    inventories.Add(spellInventory);
                }

                return inventories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get all NPC spell inventories: {ex.Message}");
                return new List<NpcSpellInventory>();
            }
        }
    }
}
