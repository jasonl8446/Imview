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

using System.Collections.ObjectModel;
using ReactiveUI;

namespace Imview.Core.Models;

/// <summary>
/// Represents a node in the WAD file tree structure.
/// </summary>
public class WadTreeNode : ReactiveObject
{
    private bool _isExpanded = false;
    private bool _isSelected = false;

    public WadTreeNode(string name, WadTreeNodeType type, string? fullPath = null)
    {
        Name = name;
        Type = type;
        FullPath = fullPath ?? name;
        Children = new ObservableCollection<WadTreeNode>();
    }

    /// <summary>
    /// Display name of the node.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Type of the node (folder or file).
    /// </summary>
    public WadTreeNodeType Type { get; }

    /// <summary>
    /// Full path to the file within the WAD (only relevant for files).
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Child nodes (for folders).
    /// </summary>
    public ObservableCollection<WadTreeNode> Children { get; }

    /// <summary>
    /// Whether this node is expanded in the tree view.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    /// <summary>
    /// Whether this node is selected in the tree view.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// Whether this node is a file (has no children).
    /// </summary>
    public bool IsFile => Type == WadTreeNodeType.File;

    /// <summary>
    /// Whether this node is a folder (can have children).
    /// </summary>
    public bool IsFolder => Type == WadTreeNodeType.Folder;

    /// <summary>
    /// Gets the file extension for file nodes.
    /// </summary>
    public string? FileExtension => IsFile ? System.IO.Path.GetExtension(Name)?.TrimStart('.') : null;

    /// <summary>
    /// Gets the file size in bytes if available.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets a formatted file size string.
    /// </summary>
    public string FileSizeFormatted
    {
        get
        {
            if (!FileSizeBytes.HasValue)
                return "";

            var bytes = FileSizeBytes.Value;
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
                _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
            };
        }
    }
}

/// <summary>
/// Type of WAD tree node.
/// </summary>
public enum WadTreeNodeType
{
    Folder,
    File
}

/// <summary>
/// Information about the currently loaded WAD file.
/// </summary>
public class WadFileInfo : ReactiveObject
{
    private string _fileName = "";
    private string _filePath = "";
    private int _totalFiles = 0;
    private long _totalSize = 0;

    /// <summary>
    /// Name of the WAD file.
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    /// <summary>
    /// Full path to the WAD file.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    /// <summary>
    /// Total number of files in the WAD.
    /// </summary>
    public int TotalFiles
    {
        get => _totalFiles;
        set => this.RaiseAndSetIfChanged(ref _totalFiles, value);
    }

    /// <summary>
    /// Total size of the WAD file in bytes.
    /// </summary>
    public long TotalSize
    {
        get => _totalSize;
        set => this.RaiseAndSetIfChanged(ref _totalSize, value);
    }

    /// <summary>
    /// Formatted total size string.
    /// </summary>
    public string TotalSizeFormatted
    {
        get
        {
            return TotalSize switch
            {
                < 1024 => $"{TotalSize} B",
                < 1024 * 1024 => $"{TotalSize / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{TotalSize / (1024.0 * 1024.0):F1} MB",
                _ => $"{TotalSize / (1024.0 * 1024.0 * 1024.0):F1} GB"
            };
        }
    }
}

/// <summary>
/// Information about the currently selected file.
/// </summary>
public class WadFileContent : ReactiveObject
{
    private string? _content;
    private bool _isDeserialized;
    private string _fileName = "";
    private string? _fileExtension;
    private long? _fileSizeBytes;

    /// <summary>
    /// The content of the file (text, JSON, or hex dump).
    /// </summary>
    public string? Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    /// <summary>
    /// Whether the content has been deserialized from binary format.
    /// </summary>
    public bool IsDeserialized
    {
        get => _isDeserialized;
        set => this.RaiseAndSetIfChanged(ref _isDeserialized, value);
    }

    /// <summary>
    /// Name of the file.
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    /// <summary>
    /// File extension.
    /// </summary>
    public string? FileExtension
    {
        get => _fileExtension;
        set => this.RaiseAndSetIfChanged(ref _fileExtension, value);
    }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long? FileSizeBytes
    {
        get => _fileSizeBytes;
        set => this.RaiseAndSetIfChanged(ref _fileSizeBytes, value);
    }

    /// <summary>
    /// Formatted file size string.
    /// </summary>
    public string FileSizeFormatted
    {
        get
        {
            if (!FileSizeBytes.HasValue)
                return "";

            var bytes = FileSizeBytes.Value;
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
                _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
            };
        }
    }

    /// <summary>
    /// Gets the content type description.
    /// </summary>
    public string ContentTypeDescription
    {
        get
        {
            if (IsDeserialized)
                return "Deserialized Object";
            
            if (string.IsNullOrEmpty(Content))
                return "Empty";

            // Check if content looks like hex dump
            if (Content.Contains(": ") && Content.Contains(" | "))
                return "Binary (Hex View)";

            return "Text";
        }
    }
}