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
using Avalonia.Controls;
using Imcodec.Wad;
using Imcodec.ObjectProperty;
using Imcodec.BCD;
using Newtonsoft.Json;

namespace Imview.Core.Services;

/// <summary>
/// Service for handling WAD file viewing operations.
/// </summary>
public static class WadViewerService
{
    private static readonly List<string> s_alwaysDeserializeExtensions = ["bin"];
    private static readonly List<string> s_sometimesDeserializeExtensions = ["xml"];
    private static readonly List<string> s_bcdExtensions = ["bcd"];
    private static readonly JsonSerializerSettings s_serializerOptions = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
    };

    /// <summary>
    /// Shows WAD selection dialog and downloads/loads the selected WAD file.
    /// </summary>
    /// <param name="parentWindow">The parent window for dialogs</param>
    /// <returns>The loaded archive and file path, or null if cancelled/failed</returns>
    public static async Task<(Archive? Archive, string? FilePath)> LoadWadFileAsync(Window parentWindow)
    {
        try
        {
            var wadSelectionService = new WadSelectionService();
            
            // Get available WADs from server
            var availableWads = await wadSelectionService.GetAvailableWadsAsync();
            if (availableWads.Count == 0)
            {
                MessageService.Error("No WAD files available. Please check your client configuration.")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .Send();
                return (null, null);
            }

            // Show WAD selection dialog
            var dialogViewModel = new ViewModels.WadSelectionDialogViewModel(availableWads);
            var selectedWad = await Views.WadSelectionDialog.ShowDialogAsync(parentWindow, dialogViewModel);
            
            if (string.IsNullOrEmpty(selectedWad))
            {
                return (null, null); // User cancelled
            }

            // Download and load the selected WAD
            var (archive, filePath) = await wadSelectionService.DownloadAndLoadWadAsync(selectedWad);
            return (archive, filePath);
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load WAD file: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
            return (null, null);
        }
    }

    /// <summary>
    /// Loads an archive from the specified file path.
    /// </summary>
    /// <param name="filePath">Path to the WAD file</param>
    /// <returns>The loaded archive and stream, or null if failed</returns>
    public static async Task<(Archive? Archive, Stream? Stream)> LoadArchiveFromFileAsync(string filePath)
    {
        try
        {
            var fileData = await File.ReadAllBytesAsync(filePath);
            // Don't dispose the stream - the Archive needs it for file access
            var archiveStream = new MemoryStream(fileData);
            var archive = ArchiveParser.Parse(archiveStream);
            return (archive, archiveStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse archive: {filePath}: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Gets the raw content of a file from the archive.
    /// </summary>
    /// <param name="archive">The archive to read from</param>
    /// <param name="fileName">The name of the file to retrieve</param>
    /// <returns>The file content as bytes, or null if not found</returns>
    public static byte[]? GetRawFileContent(Archive archive, string fileName)
    {
        try
        {
            var fileMemory = archive.OpenFile(fileName);
            if (fileMemory == null)
            {
                Console.WriteLine($"File not found in archive: {fileName}");
                return null;
            }

            // Convert Memory<byte> to byte array immediately to avoid disposed stream issues
            return fileMemory.Value.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read file {fileName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the content of a file from cached data, attempting deserialization if the file type supports it.
    /// </summary>
    /// <param name="fileName">The name of the file</param>
    /// <param name="fileData">The raw file data</param>
    /// <returns>Tuple containing the content (raw or deserialized) and whether it was deserialized</returns>
    public static (string? Content, bool IsDeserialized) GetFileContentFromData(string fileName, byte[] fileData)
    {
        if (fileData == null || fileData.Length == 0)
        {
            return (null, false);
        }

        var fileExt = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        // Handle BCD files with special BCD parser
        if (s_bcdExtensions.Contains(fileExt))
        {
            var bcdContent = TryParseBcdFile(fileName, fileData);
            if (bcdContent != null)
            {
                return (bcdContent, true);
            }
        }
        
        // Handle .bin files - always try to deserialize
        if (s_alwaysDeserializeExtensions.Contains(fileExt))
        {
            var deserializedContent = TryDeserializeWithBindSerializer(fileName, fileData);
            if (deserializedContent != null)
            {
                return (deserializedContent, true);
            }
        }
        
        // Handle .xml files - sometimes deserialize, sometimes raw
        if (s_sometimesDeserializeExtensions.Contains(fileExt))
        {
            // First try to show as readable XML
            try
            {
                var textContent = System.Text.Encoding.UTF8.GetString(fileData);
                if (IsMostlyText(textContent) && textContent.TrimStart().StartsWith('<'))
                {
                    // It's readable XML, show as-is
                    return (textContent, false);
                }
            }
            catch
            {
                // Fall through to deserialization attempt
            }
            
            // Try deserialization if it doesn't look like readable XML
            var deserializedContent = TryDeserializeWithBindSerializer(fileName, fileData);
            if (deserializedContent != null)
            {
                return (deserializedContent, true);
            }
        }

        // Return raw content as text (or hex if binary)
        try
        {
            // Try to decode as UTF-8 text first
            var textContent = System.Text.Encoding.UTF8.GetString(fileData);
            
            // Check if the content contains mostly printable characters
            if (IsMostlyText(textContent))
            {
                return (textContent, false);
            }
        }
        catch
        {
            // Fall through to hex display
        }

        // Display as hex for binary files
        var hexContent = ConvertToHexDisplay(fileData);
        return (hexContent, false);
    }

    /// <summary>
    /// Gets the content of a file, attempting deserialization if the file type supports it.
    /// </summary>
    /// <param name="archive">The archive to read from</param>
    /// <param name="fileName">The name of the file to retrieve</param>
    /// <returns>Tuple containing the content (raw or deserialized) and whether it was deserialized</returns>
    public static (string? Content, bool IsDeserialized) GetFileContent(Archive archive, string fileName)
    {
        var rawData = GetRawFileContent(archive, fileName);
        if (rawData == null)
        {
            return (null, false);
        }

        return GetFileContentFromData(fileName, rawData);
    }

    /// <summary>
    /// Attempts to parse a BCD file using Imcodec's BCD parser.
    /// </summary>
    /// <param name="fileName">The file name for metadata</param>
    /// <param name="fileData">The raw file data</param>
    /// <returns>Parsed BCD content as JSON or null if failed</returns>
    private static string? TryParseBcdFile(string fileName, byte[] fileData)
    {
        try
        {
            using var stream = new MemoryStream(fileData);
            var bcd = Bcd.Parse(stream);
            
            // Create summary of collisions without the heavy mesh data
            var collisionSummaries = bcd.Collisions.Select((collision, index) => new
            {
                index = index,
                categoryFlags = collision.CategoryFlags.ToString(),
                collisionFlags = collision.CollisionFlags.ToString(),
                geometryType = collision.Geometry.Params.TypeId,
                hasMesh = collision.Mesh != null,
                meshVertexCount = collision.Mesh?.Vertices.Count ?? 0,
                meshFaceCount = collision.Mesh?.Faces.Count ?? 0,
                // Include first few vertices for preview (limit to 5)
                meshVerticesPreview = collision.Mesh?.Vertices.Take(5).ToList()
            }).ToList();
            
            var bcdInfo = new
            {
                _fileName = fileName,
                _fileType = "BCD (Binary Collision Data)",
                _parsedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                _collisionCount = bcd.Collisions.Count,
                _imcodecVersion = typeof(Bcd).Assembly.GetName()?.Version?.ToString() ?? "Unknown",
                _note = "Large mesh data has been summarized for performance. Full mesh data is not displayed.",
                collisions = collisionSummaries
            };

            return JsonConvert.SerializeObject(bcdInfo, s_serializerOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse BCD file ({fileName}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a file using Imcodec's BindSerializer.
    /// </summary>
    /// <param name="fileName">The file name for metadata</param>
    /// <param name="fileData">The raw file data</param>
    /// <returns>Deserialized JSON content or null if failed</returns>
    private static string? TryDeserializeWithBindSerializer(string fileName, byte[] fileData)
    {
        try
        {
            var bindSerializer = new BindSerializer();
            if (bindSerializer.Deserialize<PropertyClass>(fileData, out var propertyClass))
            {
                var deserializedObject = new
                {
                    _fileName = fileName,
                    _flags = (uint)bindSerializer.SerializerFlags,
                    _className = propertyClass.GetType().Name,
                    _hash = propertyClass.GetHash(),
                    _deserializedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    _imcodecVersion = typeof(WadViewerService).Assembly.GetName()?.Version?.ToString() ?? "Unknown",
                    _object = propertyClass
                };

                return JsonConvert.SerializeObject(deserializedObject, s_serializerOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize file ({fileName}): {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Checks if a string contains mostly printable text characters.
    /// </summary>
    private static bool IsMostlyText(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        var printableCount = content.Count(c => !char.IsControl(c) || char.IsWhiteSpace(c));
        var ratio = (double)printableCount / content.Length;
        return ratio > 0.8; // 80% printable characters
    }

    /// <summary>
    /// Converts binary data to a hex dump display format.
    /// </summary>
    private static string ConvertToHexDisplay(byte[] data)
    {
        const int bytesPerLine = 16;
        var lines = new List<string>();
        
        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            var lineBytes = data.Skip(i).Take(bytesPerLine).ToArray();
            var hex = string.Join(" ", lineBytes.Select(b => b.ToString("X2")));
            var ascii = string.Join("", lineBytes.Select(b => b >= 32 && b <= 126 ? (char)b : '.'));
            
            lines.Add($"{i:X8}: {hex.PadRight(bytesPerLine * 3 - 1)} | {ascii}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}