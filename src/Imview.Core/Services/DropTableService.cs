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
using Imview.Core.Database.Collections;
using Imview.Core.Database.Models;

namespace Imview.Core.Services;

public class DropTableService {

    public async Task<List<DropTable>> GetAllDropTablesAsync() {
        return await DropTableCollection.GetAllDropTablesAsync();
    }

    public async Task<DropTable?> GetDropTableAsync(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return null;
        }

        return await DropTableCollection.GetDropTableByNameAsync(name);
    }

    public async Task<string?> CreateDropTableAsync(string name, string description = "") {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Drop table name cannot be empty.", nameof(name));
        }

        var exists = await DropTableCollection.DropTableExistsAsync(name);
        if (exists) {
            throw new InvalidOperationException($"Drop table '{name}' already exists.");
        }

        var dropTable = new DropTable {
            Name = name,
            Description = description ?? string.Empty,
            RollChance = 1.0,
            Weight = 100,
            NoneChance = 0.0,
            PityCounter = 0.0,
            MinGold = 0,
            MaxGold = 0,
            ExperienceAmount = 0,
            TrainingPoints = 0,
            Items = new List<DropItem>()
        };

        return await DropTableCollection.SaveDropTableAsync(dropTable);
    }

    public async Task<bool> UpdateDropTableAsync(DropTable dropTable) {
        if (dropTable == null) {
            throw new ArgumentNullException(nameof(dropTable));
        }

        if (string.IsNullOrWhiteSpace(dropTable.Name)) {
            throw new ArgumentException("Drop table name cannot be empty.", nameof(dropTable));
        }

        var result = await DropTableCollection.SaveDropTableAsync(dropTable);
        
        return result != null;
    }

    public async Task<bool> DeleteDropTableAsync(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return false;
        }

        var id = $"droptables/{name.ToLowerInvariant()}";
        return await DropTableCollection.DeleteDropTableAsync(id);
    }

    public async Task<List<DropTable>> SearchDropTablesAsync(string searchTerm) {
        if (string.IsNullOrWhiteSpace(searchTerm)) {
            return await GetAllDropTablesAsync();
        }

        return await DropTableCollection.SearchDropTablesAsync(searchTerm);
    }

    public void AddItemToDropTable(DropTable dropTable, string itemId, string itemName, string notes = "") {
        if (dropTable == null) {
            throw new ArgumentNullException(nameof(dropTable));
        }

        var item = new DropItem {
            ItemId = itemId ?? string.Empty,
            ItemName = itemName ?? string.Empty,
            Notes = notes ?? string.Empty
        };

        dropTable.Items.Add(item);
    }

    public void RemoveItemFromDropTable(DropTable dropTable, int index) {
        if (dropTable == null) {
            throw new ArgumentNullException(nameof(dropTable));
        }

        if (index >= 0 && index < dropTable.Items.Count) {
            dropTable.Items.RemoveAt(index);
        }
    }

    public void UpdateDropTableItem(DropTable dropTable, int index, string itemId, string itemName, string notes) {
        if (dropTable == null) {
            throw new ArgumentNullException(nameof(dropTable));
        }

        if (index >= 0 && index < dropTable.Items.Count) {
            var item = dropTable.Items[index];
            item.ItemId = itemId ?? string.Empty;
            item.ItemName = itemName ?? string.Empty;
            item.Notes = notes ?? string.Empty;
        }
    }

    public bool ValidateDropTable(DropTable dropTable, out List<string> errors) {
        errors = new List<string>();

        if (dropTable == null) {
            errors.Add("Drop table is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dropTable.Name)) {
            errors.Add("Drop table name is required.");
        }

        if (dropTable.RollChance < 0 || dropTable.RollChance > 1) {
            errors.Add("Roll chance must be between 0 and 1.");
        }

        if (dropTable.Weight < 0) {
            errors.Add("Weight must be non-negative.");
        }

        if (dropTable.NoneChance < 0 || dropTable.NoneChance > 1) {
            errors.Add("None chance must be between 0 and 1.");
        }

        if (dropTable.PityCounter < 0) {
            errors.Add("Pity counter must be non-negative.");
        }

        if (dropTable.MinGold < 0) {
            errors.Add("Min gold must be non-negative.");
        }

        if (dropTable.MaxGold < 0) {
            errors.Add("Max gold must be non-negative.");
        }

        if (dropTable.MinGold > dropTable.MaxGold && dropTable.MaxGold > 0) {
            errors.Add("Min gold cannot be greater than max gold.");
        }

        if (dropTable.ExperienceAmount < 0) {
            errors.Add("Experience amount must be non-negative.");
        }

        if (dropTable.TrainingPoints < 0) {
            errors.Add("Training points must be non-negative.");
        }

        bool hasRewards = dropTable.MinGold > 0 || dropTable.MaxGold > 0 || 
                          dropTable.ExperienceAmount > 0 || dropTable.TrainingPoints > 0 || 
                          dropTable.Items.Count > 0;

        if (!hasRewards) {
            errors.Add("Drop table must have at least one reward (gold, XP, training points, or items).");
        }

        for (int i = 0; i < dropTable.Items.Count; i++) {
            var item = dropTable.Items[i];
            if (string.IsNullOrWhiteSpace(item.ItemId) && string.IsNullOrWhiteSpace(item.ItemName)) {
                errors.Add($"Item {i + 1}: Either Item ID or Item Name is required.");
            }
        }

        return errors.Count == 0;
    }
}