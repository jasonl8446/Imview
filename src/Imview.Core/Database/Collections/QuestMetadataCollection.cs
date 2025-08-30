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
using Imview.Core.Database.Models;
using Raven.Client.Documents;

namespace Imview.Core.Database.Collections;

/// <summary>
/// Collection for managing quest metadata separately from quest templates
/// </summary>
public static class QuestMetadataCollection {

    /// <summary>
    /// Saves quest metadata to the database
    /// </summary>
    /// <param name="metadata">The quest metadata to save</param>
    /// <returns>The ID of the saved metadata document</returns>
    public static async Task<string?> SaveQuestMetadataAsync(QuestMetadata metadata) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            await session.StoreAsync(metadata);
            await session.SaveChangesAsync();

            return metadata.Id;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error saving quest metadata: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates existing quest metadata in the database
    /// </summary>
    /// <param name="metadata">The updated quest metadata</param>
    /// <returns>True if successful</returns>
    public static async Task<bool> UpdateQuestMetadataAsync(QuestMetadata metadata) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var existingMetadata = await session.LoadAsync<QuestMetadata>(metadata.Id);
            if (existingMetadata == null) {
                return false;
            }

            // Update properties on the existing tracked object
            existingMetadata.QuestTemplateId = metadata.QuestTemplateId;
            existingMetadata.Name = metadata.Name;
            existingMetadata.Description = metadata.Description;
            existingMetadata.ModifiedAt = DateTime.UtcNow;
            existingMetadata.ModifiedBy = Environment.UserName;
            await session.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error updating quest metadata (ID: {metadata.Id}, Name: {metadata.Name}): {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Retrieves quest metadata by ID
    /// </summary>
    /// <param name="metadataId">The metadata ID</param>
    /// <returns>The quest metadata or null if not found</returns>
    public static async Task<QuestMetadata?> GetQuestMetadataAsync(string metadataId) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            return await session.LoadAsync<QuestMetadata>(metadataId);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving quest metadata: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves quest metadata by quest template ID
    /// </summary>
    /// <param name="questTemplateId">The quest template ID</param>
    /// <returns>The quest metadata or null if not found</returns>
    public static async Task<QuestMetadata?> GetQuestMetadataByTemplateIdAsync(string questTemplateId) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var result = await session.Query<QuestMetadata>()
                .Where(m => m.QuestTemplateId == questTemplateId)
                .FirstOrDefaultAsync();

            return result;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving quest metadata by template ID: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves all quest metadata from the database
    /// </summary>
    /// <returns>List of quest metadata</returns>
    public static async Task<List<QuestMetadata>> GetAllQuestMetadataAsync() {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var results = await session.Query<QuestMetadata>()
                .OrderByDescending(m => m.ModifiedAt)
                .ToListAsync();

            return results;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving all quest metadata: {ex.Message}");
            return new List<QuestMetadata>();
        }
    }

    /// <summary>
    /// Searches for quest metadata by name or description
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>List of matching quest metadata</returns>
    public static async Task<List<QuestMetadata>> SearchQuestMetadataAsync(string searchTerm) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var results = await session.Query<QuestMetadata>()
                .Where(m => m.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           m.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.ModifiedAt)
                .ToListAsync();

            return results;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error searching quest metadata: {ex.Message}");
            return new List<QuestMetadata>();
        }
    }

    /// <summary>
    /// Finds quest metadata by name
    /// </summary>
    /// <param name="questName">The name of the quest to find</param>
    /// <returns>The quest metadata or null if not found</returns>
    public static async Task<QuestMetadata?> FindQuestMetadataByNameAsync(string questName) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var result = await session.Query<QuestMetadata>()
                .Where(m => m.Name == questName)
                .FirstOrDefaultAsync();

            return result;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error finding quest metadata by name: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes quest metadata from the database
    /// </summary>
    /// <param name="metadataId">The metadata ID to delete</param>
    /// <returns>True if successful</returns>
    public static async Task<bool> DeleteQuestMetadataAsync(string metadataId) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var metadata = await session.LoadAsync<QuestMetadata>(metadataId);
            if (metadata == null) {
                return false;
            }

            session.Delete(metadata);
            await session.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error deleting quest metadata: {ex.Message}");
            return false;
        }
    }
}