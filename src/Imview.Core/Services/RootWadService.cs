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
using System.Threading.Tasks;
using Imcodec.Wad;
using Imview.Core.Common;

namespace Imview.Core.Services;

/// <summary>
/// Singleton service for managing the Root.wad archive in memory.
/// </summary>
public sealed class RootWadService {
    
    private static readonly Lazy<RootWadService> _instance = new(() => new RootWadService());
    public static RootWadService Instance => _instance.Value;
    
    private Archive? _rootArchive;
    private MemoryStream? _archiveStream;
    private readonly object _lock = new();
    private readonly object _fileLock = new();
    
    private RootWadService() { }
    
    /// <summary>
    /// Gets whether the Root.wad is currently loaded in memory.
    /// </summary>
    public bool IsLoaded {
        get {
            lock (_lock) {
                return _rootArchive != null;
            }
        }
    }
    
    /// <summary>
    /// Gets the number of files in the Root.wad archive, or 0 if not loaded.
    /// </summary>
    public int FileCount {
        get {
            lock (_lock) {
                return _rootArchive?.FileCount ?? 0;
            }
        }
    }
    
    /// <summary>
    /// Loads the Root.wad from the cache if available.
    /// </summary>
    /// <returns>True if successfully loaded, false if not found or failed to load.</returns>
    public async Task<bool> LoadFromCacheAsync() {
        try {
            var clientFileService = new ClientFileService();
            var selectedRevision = clientFileService.GetSelectedRevision();
            
            if (string.IsNullOrEmpty(selectedRevision)) {
                return false;
            }
            
            var cacheDir = clientFileService.GetCacheDirectory();
            var rootWadPath = Path.Combine(cacheDir, selectedRevision, "Root.wad");
            
            if (!File.Exists(rootWadPath)) {
                return false;
            }
            
            return await LoadFromFileAsync(rootWadPath);
        }
        catch (Exception ex) {
            Console.WriteLine($"Failed to load Root.wad from cache: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Loads the Root.wad from a specific file path.
    /// </summary>
    /// <param name="filePath">The path to the Root.wad file.</param>
    /// <returns>True if successfully loaded, false if failed to load.</returns>
    public async Task<bool> LoadFromFileAsync(string filePath) {
        try {
            if (!File.Exists(filePath)) {
                return false;
            }
            
            Archive? newArchive;
            MemoryStream? newArchiveStream;
            
            // Open and parse the WAD file
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            newArchiveStream = new MemoryStream();
            
            // Copy to memory stream for better performance and thread safety
            await fileStream.CopyToAsync(newArchiveStream);
            newArchiveStream.Position = 0;
            
            newArchive = ArchiveParser.Parse(newArchiveStream);
            
            if (newArchive == null) {
                newArchiveStream.Dispose();
                return false;
            }
            
            // Atomically replace the current archive and dispose the old stream
            lock (_lock) {
                _archiveStream?.Dispose();
                _rootArchive = newArchive;
                _archiveStream = newArchiveStream;
            }
            
            Console.WriteLine($"Root.wad loaded successfully: {newArchive.FileCount} files, {newArchive.Size()} bytes");
            
            // Automatically load locale data after Root.wad is loaded
            _ = Task.Run(() => {
                try {
                    LocaleService.Instance.LoadFromRootWad();
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to load locale data: {ex.Message}");
                }
            });
            
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Failed to load Root.wad from {filePath}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets a file from the Root.wad archive.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <returns>The file data as a memory block, or null if not found or archive not loaded.</returns>
    public Memory<byte>? GetFile(string fileName) {
        lock (_fileLock) {
            lock (_lock) {
                return _rootArchive?.OpenFile(fileName);
            }
        }
    }
    
    /// <summary>
    /// Gets a file from the Root.wad archive asynchronously.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <returns>The file data as a memory block, or null if not found or archive not loaded.</returns>
    public async Task<Memory<byte>?> GetFileAsync(string fileName) {
        // For async, we need to use TaskCompletionSource to avoid holding locks across await
        var tcs = new TaskCompletionSource<Memory<byte>?>();
        
        _ = Task.Run(() => {
            try {
                var result = GetFile(fileName);
                tcs.SetResult(result);
            }
            catch (Exception ex) {
                tcs.SetException(ex);
            }
        });
        
        return await tcs.Task;
    }
    
    /// <summary>
    /// Checks if the Root.wad archive contains a specific file.
    /// </summary>
    /// <param name="fileName">The name of the file to check for.</param>
    /// <returns>True if the file exists in the archive, false otherwise.</returns>
    public bool ContainsFile(string fileName) {
        lock (_lock) {
            return _rootArchive?.ContainsFile(fileName) ?? false;
        }
    }
    
    /// <summary>
    /// Gets all file names in the Root.wad archive.
    /// </summary>
    /// <returns>An enumerable of file names, or empty if archive not loaded.</returns>
    public IEnumerable<string> GetFileNames() {
        lock (_lock) {
            return _rootArchive?.Files.Keys.ToList() ?? Enumerable.Empty<string>();
        }
    }
    
    /// <summary>
    /// Unloads the Root.wad archive from memory.
    /// </summary>
    public void Unload() {
        lock (_lock) {
            _archiveStream?.Dispose();
            _rootArchive = null;
            _archiveStream = null;
        }
    }
    
    /// <summary>
    /// Gets information about the Root.wad archive.
    /// </summary>
    public RootWadInfo GetInfo() {
        lock (_lock) {
            if (_rootArchive == null) {
                return new RootWadInfo { IsLoaded = false, FileCount = 0, SizeBytes = 0 };
            }
            
            return new RootWadInfo {
                IsLoaded = true,
                FileCount = _rootArchive.FileCount,
                SizeBytes = _rootArchive.Size()
            };
        }
    }
}

/// <summary>
/// Information about the Root.wad archive status.
/// </summary>
public class RootWadInfo {
    public bool IsLoaded { get; init; }
    public int FileCount { get; init; }
    public uint SizeBytes { get; init; }
    
    public string SizeFormatted => SizeBytes switch {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{SizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
    };
}