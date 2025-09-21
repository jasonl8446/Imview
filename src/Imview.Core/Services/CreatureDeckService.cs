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
using Imview.Core.Database.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Imview.Core.Services;

/// <summary>
/// Service for managing creature spell decks in the world database
/// Uses RavenDB following the same pattern as other Imview services
/// </summary>
public static class CreatureDeckService
{
    /// <summary>
    /// Gets the creature deck for a specific deck name
    /// </summary>
    /// <param name="deckName">The deck name to look up</param>
    /// <returns>The creature deck if found, null otherwise</returns>
public static async Task<CreatureDeck?> GetCreatureDeckAsync(string deckName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deckName))
                return null;

            var store = WorldDatabase.Instance.Store;
            if (store == null)
                return null;

            // Use synchronous session and a simple RQL query
            using IDocumentSession session = store.OpenSession();
            var doc = session.Advanced.RawQuery<CreatureSpellbook>(
                    "from CreatureSpellbook where DeckName = $deckName")
                .AddParameter("deckName", deckName)
                .FirstOrDefault();

            if (doc == null)
            {
                Console.WriteLine($"[DEBUG] Creature deck '{deckName}' not found in CreatureSpellbook collection");
                return null;
            }

            var deck = new CreatureDeck
            {
                DeckName = doc.DeckName,
                SpellTemplateIds = doc.SpellTemplateIds?.Select(id => (uint)id).ToList() ?? new List<uint>()
            };

            Console.WriteLine($"[DEBUG] Loaded creature deck '{deckName}': {deck.SpellTemplateIds.Count} spells");
            return deck;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to get creature deck '{deckName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves a creature deck to the database
    /// </summary>
    /// <param name="deck">The deck to save</param>
    /// <returns>True if successful, false otherwise</returns>
public static async Task<bool> SaveCreatureDeckAsync(CreatureDeck deck)
    {
        try
        {
            if (deck == null || string.IsNullOrWhiteSpace(deck.DeckName))
                return false;

            var store = WorldDatabase.Instance.Store;
            if (store == null)
                return false;

            using IDocumentSession session = store.OpenSession();

            // Try find existing document by DeckName
            var existing = session.Advanced.RawQuery<CreatureSpellbook>(
                    "from CreatureSpellbook where DeckName = $deckName")
                .AddParameter("deckName", deck.DeckName)
                .FirstOrDefault();

            if (existing != null)
            {
                existing.SpellTemplateIds = deck.SpellTemplateIds.Select(id => (long)id).ToList();
                session.Store(existing); // track changes
                Console.WriteLine($"[INFO] Updated existing creature deck '{deck.DeckName}' with {deck.SpellTemplateIds.Count} spells");
            }
            else
            {
                var doc = new CreatureSpellbook
                {
                    DeckName = deck.DeckName,
                    SpellTemplateIds = deck.SpellTemplateIds.Select(id => (long)id).ToList()
                };
                // Use deterministic ID and set collection explicitly
                session.Store(doc, $"CreatureSpellbook/{deck.DeckName}");
                session.Advanced.GetMetadataFor(doc)["@collection"] = "CreatureSpellbook";
                Console.WriteLine($"[INFO] Created new creature deck '{deck.DeckName}' with {deck.SpellTemplateIds.Count} spells");
            }

            session.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to save creature deck '{deck?.DeckName}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes a creature deck from the database
    /// </summary>
    /// <param name="deckName">The deck name to delete</param>
    /// <returns>True if successful, false otherwise</returns>
public static async Task<bool> DeleteCreatureDeckAsync(string deckName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deckName))
                return false;

            var store = WorldDatabase.Instance.Store;
            if (store == null)
                return false;

            using IDocumentSession session = store.OpenSession();

            var existing = session.Advanced.RawQuery<CreatureSpellbook>(
                    "from CreatureSpellbook where DeckName = $deckName")
                .AddParameter("deckName", deckName)
                .FirstOrDefault();

            if (existing != null)
            {
                var docId = session.Advanced.GetDocumentId(existing);
                session.Delete(docId);
                session.SaveChanges();
                Console.WriteLine($"[INFO] Deleted creature deck '{deckName}' (doc ID: {docId})");
                return true;
            }

            Console.WriteLine($"[DEBUG] Creature deck '{deckName}' not found for deletion");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to delete creature deck '{deckName}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a creature deck exists
    /// </summary>
    /// <param name="deckName">The deck name to check</param>
    /// <returns>True if the deck exists, false otherwise</returns>
public static async Task<bool> HasCreatureDeckAsync(string deckName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deckName))
                return false;

            var store = WorldDatabase.Instance.Store;
            if (store == null)
                return false;

            using IDocumentSession session = store.OpenSession();
            var count = session.Advanced.RawQuery<CreatureSpellbook>(
                    "from CreatureSpellbook where DeckName = $deckName select DeckName")
                .AddParameter("deckName", deckName)
                .Count();

            var exists = count > 0;
            Console.WriteLine($"[DEBUG] Deck existence check for '{deckName}': {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to check creature deck existence '{deckName}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all creature deck names
    /// </summary>
    /// <returns>List of all deck names</returns>
public static async Task<List<string>> GetAllDeckNamesAsync()
    {
        try
        {
            var store = WorldDatabase.Instance.Store;
            if (store == null)
                return new List<string>();

            using IDocumentSession session = store.OpenSession();

            var results = session.Advanced.RawQuery<CreatureSpellbook>(
                    "from CreatureSpellbook select DeckName")
                .ToList();

            var deckNames = results
                .Select(r => r.DeckName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return deckNames;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to get all deck names: {ex.Message}");
            return new List<string>();
        }
    }
}