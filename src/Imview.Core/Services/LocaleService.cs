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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imview.Core.Services;

/// <summary>
/// Singleton service for managing localized text from .lang files in Root.wad.
/// </summary>
public sealed class LocaleService {
    
    private static readonly Lazy<LocaleService> _instance = new(() => new LocaleService());
    public static LocaleService Instance => _instance.Value;
    
    private readonly Dictionary<string, Dictionary<string, string>> _localeData = new();
    private readonly object _lock = new();
    
    private LocaleService() { }
    
    /// <summary>
    /// Gets whether locale data has been loaded.
    /// </summary>
    public bool IsLoaded {
        get {
            lock (_lock) {
                return _localeData.Count > 0;
            }
        }
    }
    
    /// <summary>
    /// Gets the number of loaded locale categories.
    /// </summary>
    public int CategoryCount {
        get {
            lock (_lock) {
                return _localeData.Count;
            }
        }
    }
    
    /// <summary>
    /// Gets the total number of locale strings across all categories.
    /// </summary>
    public int TotalStringCount {
        get {
            lock (_lock) {
                return _localeData.Values.Sum(category => category.Count);
            }
        }
    }
    
    /// <summary>
    /// Loads all .lang files from Root.wad's Locale/English directory.
    /// </summary>
    /// <returns>True if successfully loaded, false if Root.wad not available or failed to load.</returns>
    public Task<bool> LoadFromRootWadAsync() {
        return Task.Run(() => LoadFromRootWad());
    }
    
    /// <summary>
    /// Loads all .lang files from Root.wad's Locale/English directory synchronously.
    /// </summary>
    /// <returns>True if successfully loaded, false if Root.wad not available or failed to load.</returns>
    public bool LoadFromRootWad() {
        if (!RootWadService.Instance.IsLoaded) {
            return false;
        }
        
        try {
            var loadedCategories = 0;
            var loadedStrings = 0;
            
            // Get list of all files in the Root.wad
            var fileNames = RootWadService.Instance.GetFileNames();
            var langFiles = fileNames.Where(f => f.StartsWith("Locale/English/", StringComparison.OrdinalIgnoreCase) 
                                                && f.EndsWith(".lang", StringComparison.OrdinalIgnoreCase))
                                     .ToList();
            
            lock (_lock) {
                _localeData.Clear();
            }
            
            foreach (var langFile in langFiles) {
                try {
                    var fileData = RootWadService.Instance.GetFile(langFile);
                    
                    if (fileData.HasValue) {
                        var category = ParseLangFile(langFile, fileData.Value);
                        if (category != null) {
                            lock (_lock) {
                                _localeData[category.Name] = category.Entries;
                            }
                            loadedCategories++;
                            loadedStrings += category.Entries.Count;
                            
                            // Progress reporting for large loads
                            if (loadedCategories % 50 == 0) {
                                Console.WriteLine($"Loaded {loadedCategories}/{langFiles.Count} locale categories...");
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to load {langFile}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"Loaded {loadedCategories} locale categories with {loadedStrings} strings total");
            return loadedCategories > 0;
        }
        catch (Exception ex) {
            Console.WriteLine($"Failed to load locale data: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets a localized string from a specific category by key.
    /// </summary>
    /// <param name="category">The category name (e.g., "Spells", "Items", "WizQst15511B").</param>
    /// <param name="key">The string key (e.g., "00000001").</param>
    /// <returns>The localized string, or null if not found.</returns>
    public string? GetString(string category, string key) {
        lock (_lock) {
            if (_localeData.TryGetValue(category, out var categoryData)) {
                return categoryData.TryGetValue(key, out var value) ? value : null;
            }
            return null;
        }
    }
    
    /// <summary>
    /// Gets a localized string from a specific category by numeric key.
    /// </summary>
    /// <param name="category">The category name (e.g., "Spells", "Items").</param>
    /// <param name="numericKey">The numeric key (e.g., 1 for "00000001").</param>
    /// <returns>The localized string, or null if not found.</returns>
    public string? GetString(string category, int numericKey) {
        var paddedKey = numericKey.ToString("00000000");
        return GetString(category, paddedKey);
    }
    
    /// <summary>
    /// Searches for strings containing the specified text across all categories.
    /// </summary>
    /// <param name="searchText">The text to search for (case-insensitive).</param>
    /// <param name="maxResults">Maximum number of results to return (default 100).</param>
    /// <returns>A list of matching locale entries.</returns>
    public List<LocaleEntry> SearchStrings(string searchText, int maxResults = 100) {
        if (string.IsNullOrWhiteSpace(searchText)) {
            return new List<LocaleEntry>();
        }
        
        var results = new List<LocaleEntry>();
        var searchLower = searchText.ToLowerInvariant();
        
        lock (_lock) {
            foreach (var category in _localeData) {
                foreach (var entry in category.Value) {
                    if (entry.Value.Contains(searchLower, StringComparison.OrdinalIgnoreCase)) {
                        results.Add(new LocaleEntry {
                            Category = category.Key,
                            Key = entry.Key,
                            Value = entry.Value
                        });
                        
                        if (results.Count >= maxResults) {
                            return results;
                        }
                    }
                }
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Gets all strings from a specific category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>A dictionary of key-value pairs, or empty dictionary if category not found.</returns>
    public Dictionary<string, string> GetCategory(string category) {
        lock (_lock) {
            return _localeData.TryGetValue(category, out var categoryData) 
                ? new Dictionary<string, string>(categoryData) 
                : new Dictionary<string, string>();
        }
    }
    
    /// <summary>
    /// Gets a list of all available category names.
    /// </summary>
    /// <returns>A list of category names.</returns>
    public List<string> GetCategoryNames() {
        lock (_lock) {
            return _localeData.Keys.ToList();
        }
    }
    
    /// <summary>
    /// Checks if a category exists.
    /// </summary>
    /// <param name="category">The category name to check.</param>
    /// <returns>True if the category exists, false otherwise.</returns>
    public bool HasCategory(string category) {
        lock (_lock) {
            return _localeData.ContainsKey(category);
        }
    }
    
    /// <summary>
    /// Gets information about the loaded locale data.
    /// </summary>
    public LocaleInfo GetInfo() {
        lock (_lock) {
            return new LocaleInfo {
                IsLoaded = _localeData.Count > 0,
                CategoryCount = _localeData.Count,
                TotalStringCount = _localeData.Values.Sum(cat => cat.Count),
                Categories = _localeData.Keys.ToList()
            };
        }
    }
    
    /// <summary>
    /// Clears all loaded locale data.
    /// </summary>
    public void Clear() {
        lock (_lock) {
            _localeData.Clear();
        }
    }
    
    private LangCategory? ParseLangFile(string fileName, Memory<byte> data) {
        try {
            // Extract category name from file path (e.g., "Locale/English/Spells.lang" -> "Spells")
            var categoryName = Path.GetFileNameWithoutExtension(fileName);
            
            // Convert bytes to UTF-16 string (lang files are UTF-16 encoded)
            var content = Encoding.Unicode.GetString(data.Span);
            
            var entries = new Dictionary<string, string>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < lines.Length; i++) {
                var line = lines[i].Trim();
                
                // Skip empty lines and header line
                if (string.IsNullOrEmpty(line) || line.Contains(':')) {
                    continue;
                }
                
                // Check if this line looks like a key (all digits and spaces)
                if (IsKeyLine(line)) {
                    var key = line.Replace(" ", ""); // Remove spaces from key
                    
                    // Get the value from the next non-empty line
                    if (i + 1 < lines.Length) {
                        var valueLine = lines[i + 1].Trim();
                        if (!string.IsNullOrEmpty(valueLine) && !IsKeyLine(valueLine)) {
                            // Clean up the value (remove extra spaces between characters)
                            var cleanValue = CleanValue(valueLine);
                            entries[key] = cleanValue;
                        }
                    }
                }
            }
            
            return entries.Count > 0 ? new LangCategory { Name = categoryName, Entries = entries } : null;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error parsing {fileName}: {ex.Message}");
            return null;
        }
    }
    
    private static bool IsKeyLine(string line) {
        // Key lines are all digits separated by spaces (e.g., "0 0 0 0 0 0 0 1")
        var withoutSpaces = line.Replace(" ", "");
        return withoutSpaces.Length == 8 && withoutSpaces.All(char.IsDigit);
    }
    
    private static string CleanValue(string value) {
        // The UTF-16 encoding might have spaces between characters, clean them up
        var cleaned = new StringBuilder();
        bool lastWasSpace = false;
        
        foreach (char c in value) {
            if (c == ' ') {
                if (!lastWasSpace) {
                    cleaned.Append(c);
                    lastWasSpace = true;
                }
            } else {
                cleaned.Append(c);
                lastWasSpace = false;
            }
        }
        
        return cleaned.ToString().Trim();
    }
}

/// <summary>
/// Represents a parsed .lang file category.
/// </summary>
internal class LangCategory {
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Entries { get; set; } = new();
}

/// <summary>
/// Represents a single locale entry.
/// </summary>
public class LocaleEntry {
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    
    public int NumericKey => int.TryParse(Key, out int result) ? result : 0;
}

/// <summary>
/// Information about the loaded locale data.
/// </summary>
public class LocaleInfo {
    public bool IsLoaded { get; init; }
    public int CategoryCount { get; init; }
    public int TotalStringCount { get; init; }
    public List<string> Categories { get; init; } = new();
}