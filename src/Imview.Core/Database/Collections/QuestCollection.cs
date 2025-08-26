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
using Imcodec.ObjectProperty.TypeCache;
using Raven.Client.Documents;
using Raven.Client.Documents.Queries;

namespace Imview.Core.Database.Collections;

/// <summary>
/// Represents a quest document stored in RavenDB with metadata
/// </summary>
public class QuestDocument {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public QuestTemplate Template { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = Environment.UserName;
    public string ModifiedBy { get; set; } = Environment.UserName;
}

/// <summary>
/// Collection for managing quest templates in the WorldDB database
/// </summary>
public static class QuestCollection {

    /// <summary>
    /// Saves a quest template to the database
    /// </summary>
    /// <param name="questTemplate">The quest template to save</param>
    /// <param name="name">The name for the quest</param>
    /// <param name="description">Optional description</param>
    /// <returns>The ID of the saved quest document</returns>
    public static async Task<string?> SaveQuestAsync(QuestTemplate questTemplate, string name, string description = "") {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var questDoc = new QuestDocument {
                Name = name,
                Description = description,
                Template = questTemplate,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                CreatedBy = Environment.UserName,
                ModifiedBy = Environment.UserName
            };

            await session.StoreAsync(questDoc);
            await session.SaveChangesAsync();

            return questDoc.Id;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error saving quest: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates an existing quest template in the database
    /// </summary>
    /// <param name="questId">The ID of the quest to update</param>
    /// <param name="questTemplate">The updated quest template</param>
    /// <param name="name">The updated name</param>
    /// <param name="description">The updated description</param>
    /// <returns>True if successful</returns>
    public static async Task<bool> UpdateQuestAsync(string questId, QuestTemplate questTemplate, string name, string description = "") {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var questDoc = await session.LoadAsync<QuestDocument>(questId);
            if (questDoc == null) {
                return false;
            }

            questDoc.Name = name;
            questDoc.Description = description;
            questDoc.Template = questTemplate;
            questDoc.ModifiedAt = DateTime.UtcNow;
            questDoc.ModifiedBy = Environment.UserName;

            await session.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error updating quest: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Retrieves a quest template by ID
    /// </summary>
    /// <param name="questId">The quest ID</param>
    /// <returns>The quest document or null if not found</returns>
    public static async Task<QuestDocument?> GetQuestAsync(string questId) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            return await session.LoadAsync<QuestDocument>(questId);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving quest: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves all quest templates from the database
    /// </summary>
    /// <returns>List of quest documents</returns>
    public static async Task<List<QuestDocument>> GetAllQuestsAsync() {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var results = await session.Query<QuestDocument>()
                .OrderByDescending(q => q.ModifiedAt)
                .ToListAsync();

            return results;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving all quests: {ex.Message}");
            return new List<QuestDocument>();
        }
    }

    /// <summary>
    /// Searches for quest templates by name
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>List of matching quest documents</returns>
    public static async Task<List<QuestDocument>> SearchQuestsAsync(string searchTerm) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var results = await session.Query<QuestDocument>()
                .Where(q => q.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           q.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.ModifiedAt)
                .ToListAsync();

            return results;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error searching quests: {ex.Message}");
            return new List<QuestDocument>();
        }
    }

    /// <summary>
    /// Finds a quest by name
    /// </summary>
    /// <param name="questName">The name of the quest to find</param>
    /// <returns>The quest document or null if not found</returns>
    public static async Task<QuestDocument?> FindQuestByNameAsync(string questName) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var result = await session.Query<QuestDocument>()
                .Where(q => q.Name == questName)
                .FirstOrDefaultAsync();

            return result;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error finding quest by name: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Upserts a quest template - creates new if not exists, updates if exists
    /// </summary>
    /// <param name="questTemplate">The quest template</param>
    /// <param name="name">The name for the quest</param>
    /// <param name="description">Optional description</param>
    /// <returns>The ID of the upserted quest document</returns>
    public static async Task<string?> UpsertQuestAsync(QuestTemplate questTemplate, string name, string description = "") {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            // Try to find existing quest by name
            // First, get all quests and check if name exists (for debugging)
            var allQuests = await session.Query<QuestDocument>().ToListAsync();
            var existingQuestByName = allQuests.FirstOrDefault(q => q.Name == name);
            
            // Also try the original query approach
            var existingQuest = await session.Query<QuestDocument>()
                .Where(q => q.Name == name)
                .FirstOrDefaultAsync();

            // Use the in-memory search result as it's more reliable
            existingQuest = existingQuestByName;
            
            if (existingQuest != null) {
                // Completely replace the existing quest while preserving the ID and creation info
                string preservedId = existingQuest.Id;
                DateTime preservedCreatedAt = existingQuest.CreatedAt;
                string preservedCreatedBy = existingQuest.CreatedBy;
                
                // Replace all properties with new data
                existingQuest.Name = name;
                existingQuest.Description = description;
                existingQuest.Template = questTemplate;
                existingQuest.ModifiedAt = DateTime.UtcNow;
                existingQuest.ModifiedBy = Environment.UserName;
                
                // Ensure we preserve the original creation metadata
                existingQuest.Id = preservedId;
                existingQuest.CreatedAt = preservedCreatedAt;
                existingQuest.CreatedBy = preservedCreatedBy;
                
                await session.SaveChangesAsync();
                return existingQuest.Id;
            } else {
                // Create new quest
                var questDoc = new QuestDocument {
                    Name = name,
                    Description = description,
                    Template = questTemplate,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    CreatedBy = Environment.UserName,
                    ModifiedBy = Environment.UserName
                };

                await session.StoreAsync(questDoc);
                await session.SaveChangesAsync();
                return questDoc.Id;
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error upserting quest: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes a quest template from the database
    /// </summary>
    /// <param name="questId">The quest ID to delete</param>
    /// <returns>True if successful</returns>
    public static async Task<bool> DeleteQuestAsync(string questId) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                throw new InvalidOperationException("Database connection not available");
            }

            using var session = store.OpenAsyncSession();
            
            var questDoc = await session.LoadAsync<QuestDocument>(questId);
            if (questDoc == null) {
                return false;
            }

            session.Delete(questDoc);
            await session.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error deleting quest: {ex.Message}");
            return false;
        }
    }

}