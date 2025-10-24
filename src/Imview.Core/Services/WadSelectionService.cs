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
using Imview.Core.Models;

namespace Imview.Core.Services;

/// <summary>
/// Service for handling WAD file selection and downloading from the server.
/// </summary>
public class WadSelectionService
{
    private readonly ClientFileService _clientFileService;

    public WadSelectionService()
    {
        _clientFileService = new ClientFileService();
    }

    /// <summary>
    /// Gets a list of available WAD files from the server.
    /// </summary>
    /// <returns>List of WAD file names</returns>
    public async Task<List<string>> GetAvailableWadsAsync()
    {
        try
        {
            var selectedRevision = _clientFileService.GetSelectedRevision();
            if (string.IsNullOrEmpty(selectedRevision))
            {
                Console.WriteLine("No revision selected for WAD listing");
                return new List<string>();
            }

            Console.WriteLine($"Fetching file list for revision: {selectedRevision}");
            var fileList = await _clientFileService.FetchFileListAsync(selectedRevision);
            
            // Filter for WAD files
            var wadFiles = fileList.Records
                .Where(record => record.SourceFileName.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
                .Select(record => Path.GetFileName(record.SourceFileName))
                .Where(fileName => !string.IsNullOrEmpty(fileName))
                .OrderBy(fileName => fileName)
                .ToList();

            Console.WriteLine($"Found {wadFiles.Count} WAD files in revision {selectedRevision}");
            Console.WriteLine($"First 10 WADs: {string.Join(", ", wadFiles.Take(10))}");
            
            return wadFiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get available WADs: {ex.Message}");
            throw new InvalidOperationException($"Failed to get available WAD files: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Downloads and loads a WAD file from the server.
    /// </summary>
    /// <param name="wadFileName">Name of the WAD file to download</param>
    /// <returns>The loaded archive and file path, or null if failed</returns>
    public async Task<(Archive? Archive, string? FilePath)> DownloadAndLoadWadAsync(string wadFileName)
    {
        try
        {
            var selectedRevision = _clientFileService.GetSelectedRevision();
            if (string.IsNullOrEmpty(selectedRevision))
            {
                throw new InvalidOperationException("No revision selected for WAD download");
            }

            Console.WriteLine($"Loading WAD: {wadFileName} from revision: {selectedRevision}");
            
            // First check if already cached
            var cacheDir = _clientFileService.GetCacheDirectory();
            var cachedWadPath = Path.Combine(cacheDir, selectedRevision, wadFileName);
            var cachedWadPathWithPrefix = Path.Combine(cacheDir, selectedRevision, "Data", "GameData", wadFileName);
            
            Console.WriteLine($"Checking cache path: {cachedWadPath}");
            Console.WriteLine($"Checking cache path with prefix: {cachedWadPathWithPrefix}");
            
            string? localFilePath = null;
            
            if (File.Exists(cachedWadPath))
            {
                localFilePath = cachedWadPath;
                Console.WriteLine($"Found cached WAD: {localFilePath}");
            }
            else if (File.Exists(cachedWadPathWithPrefix))
            {
                localFilePath = cachedWadPathWithPrefix;
                Console.WriteLine($"Found cached WAD with prefix: {localFilePath}");
            }
            else
            {
                // Need to download
                Console.WriteLine($"WAD not cached, downloading: {wadFileName}");
                
                var fileList = await _clientFileService.FetchFileListAsync(selectedRevision);
                var wadFileRecord = fileList.Records.FirstOrDefault(r => 
                    Path.GetFileName(r.SourceFileName).Equals(wadFileName, StringComparison.OrdinalIgnoreCase));
                    
                if (wadFileRecord == null)
                {
                    throw new FileNotFoundException($"WAD file '{wadFileName}' not found in revision {selectedRevision}");
                }
                
                Console.WriteLine($"Found WAD file record: {wadFileRecord.SourceFileName}");
                
                // Download the WAD using the existing download infrastructure
                localFilePath = await _clientFileService.DownloadFileAsync(wadFileRecord, selectedRevision);
                Console.WriteLine($"Downloaded to: {localFilePath}");
            }
            
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"WAD file does not exist at: {localFilePath}");
            }
            
            // Load the archive
            var fileData = await File.ReadAllBytesAsync(localFilePath);
            using var archiveStream = new MemoryStream(fileData);
            var archive = ArchiveParser.Parse(archiveStream);
            
            if (archive == null)
            {
                throw new InvalidOperationException($"Failed to parse WAD file: {wadFileName}");
            }
            
            Console.WriteLine($"Successfully loaded WAD: {wadFileName} ({archive.FileCount} files)");
            return (archive, localFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download/load WAD {wadFileName}: {ex.Message}");
            throw new InvalidOperationException($"Failed to load WAD '{wadFileName}': {ex.Message}", ex);
        }
    }
}