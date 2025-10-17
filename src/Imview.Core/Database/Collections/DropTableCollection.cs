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
using Imcodec.ObjectProperty.TypeCache;

namespace Imview.Core.Database.Collections;

public static class DropTableCollection {

    public static async Task<string?> SaveDropTableAsync(DropTable dropTable) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                Console.WriteLine("Database store is not available.");
                return null;
            }

            using var session = store.OpenAsyncSession();
            
            var id = $"droptables/{dropTable.Name.ToLowerInvariant()}";
            
            // Find existing table using LINQ query
            var existing = await session.Query<DropTable>()
                .Where(dt => dt.Name == dropTable.Name)
                .FirstOrDefaultAsync();
            
            if (existing != null) {
                // Update existing - copy data to the tracked entity
                existing.Description = dropTable.Description;
                existing.RollChance = dropTable.RollChance;
                existing.Weight = dropTable.Weight;
                existing.NoneChance = dropTable.NoneChance;
                existing.PityCounter = dropTable.PityCounter;
                existing.Items = dropTable.Items;
                existing.ModifiedAt = DateTime.UtcNow;
                existing.ModifiedBy = Environment.UserName;
                existing.MinGold = dropTable.MinGold;
                existing.MaxGold = dropTable.MaxGold;
                existing.ExperienceAmount = dropTable.ExperienceAmount;
                existing.TrainingPoints = dropTable.TrainingPoints;
            } else {
                // Create new
                dropTable.Id = id;
                dropTable.CreatedAt = DateTime.UtcNow;
                dropTable.CreatedBy = Environment.UserName;
                dropTable.ModifiedAt = DateTime.UtcNow;
                dropTable.ModifiedBy = Environment.UserName;
                dropTable.PityCounter = dropTable.PityCounter;
                dropTable.Items = dropTable.Items;
                dropTable.MinGold = dropTable.MinGold;
                dropTable.MaxGold = dropTable.MaxGold;
                dropTable.ExperienceAmount = dropTable.ExperienceAmount;
                dropTable.TrainingPoints = dropTable.TrainingPoints;

                await session.StoreAsync(dropTable, id);
            }

            await session.SaveChangesAsync();
            return id;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error saving drop table: {ex.Message}");
            return null;
        }
    }

    public static async Task<bool> UpdateDropTableAsync(DropTable dropTable) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                Console.WriteLine("Database store is not available.");
                return false;
            }

            using var session = store.OpenAsyncSession();
            
            dropTable.ModifiedAt = DateTime.UtcNow;
            dropTable.ModifiedBy = Environment.UserName;

            await session.StoreAsync(dropTable, dropTable.Id);
            await session.SaveChangesAsync();

            Console.WriteLine($"Drop table '{dropTable.Name}' updated successfully.");
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error updating drop table: {ex.Message}");
            return false;
        }
    }

    public static async Task<DropTable?> GetDropTableAsync(string id) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                Console.WriteLine("Database store is not available.");
                return null;
            }

            using var session = store.OpenAsyncSession();
            return await session.LoadAsync<DropTable>(id);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving drop table: {ex.Message}");
            return null;
        }
    }

    public static async Task<DropTable?> GetDropTableByNameAsync(string name) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                Console.WriteLine("Database store is not available.");
                return null;
            }

            using var session = store.OpenAsyncSession();
            var id = $"droptables/{name.ToLowerInvariant()}";
            return await session.LoadAsync<DropTable>(id);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving drop table by name: {ex.Message}");
            return null;
        }
    }

    public static async Task<List<DropTable>> GetAllDropTablesAsync() {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                Console.WriteLine("Database store is not available.");
                return new List<DropTable>();
            }

            using var session = store.OpenAsyncSession();
            var dropTables = await session.Query<DropTable>()
                .Where(dt => dt.Id.StartsWith("droptables/"))
                .OrderBy(dt => dt.Name)
                .ToListAsync();

            return dropTables;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error retrieving all drop tables: {ex.Message}");
            return new List<DropTable>();
        }
    }

    public static async Task<List<DropTable>> SearchDropTablesAsync(string searchTerm) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                Console.WriteLine("Database store is not available.");
                return new List<DropTable>();
            }

            using var session = store.OpenAsyncSession();
            var dropTables = await session.Query<DropTable>()
                .Where(dt => dt.Id.StartsWith("droptables/") && 
                           dt.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(dt => dt.Name)
                .ToListAsync();

            return dropTables;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error searching drop tables: {ex.Message}");
            return new List<DropTable>();
        }
    }

    public static async Task<bool> DeleteDropTableAsync(string id) {
        try {
            var store = WorldDatabase.Instance.Store;
            if (store == null) {
                Console.WriteLine("Database store is not available.");
                return false;
            }

            using var session = store.OpenAsyncSession();
            session.Delete(id);
            await session.SaveChangesAsync();

            Console.WriteLine($"Drop table with ID '{id}' deleted successfully.");
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error deleting drop table: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> DropTableExistsAsync(string name) {
        try {
            var dropTable = await GetDropTableByNameAsync(name);
            return dropTable != null;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error checking if drop table exists: {ex.Message}");
            return false;
        }
    }
}