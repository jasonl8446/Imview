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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Imcodec.Wad;
using Imview.Core.Models;
using Imview.Core.Services;
using ReactiveUI;

namespace Imview.Core.ViewModels;

public class WadViewerViewModel : ViewModelBase, IDisposable
{
    private readonly MainWindowViewModel? _mainViewModel;
    private Archive? _currentArchive;
    private Stream? _archiveStream; // Keep stream alive for archive access
    private Dictionary<string, byte[]> _fileDataCache = new();
    private WadFileInfo _wadInfo = new();
    private WadFileContent _fileContent = new();
    private ObservableCollection<WadTreeNode> _treeNodes = new();
    private WadTreeNode? _selectedNode;
    private bool _isLoading = false;
    private string _statusText = "No WAD file loaded";
    private bool _autoPromptedWadSelection = false;

    public WadViewerViewModel(MainWindowViewModel? mainViewModel = null)
    {
        _mainViewModel = mainViewModel;
        LoadWadCommand = ReactiveCommand.CreateFromTask(LoadWadFileAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshCurrentWadAsync);
        
        // Subscribe to node selection changes
        this.WhenAnyValue(x => x.SelectedNode)
            .Subscribe(async node => await OnNodeSelectionChanged(node));

        // Auto-prompt for WAD selection on first open (similar to Zone Editor)
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!_autoPromptedWadSelection && !HasWadLoaded)
            {
                _autoPromptedWadSelection = true;
                _ = LoadWadFileAsync();
            }
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    #region Properties

    /// <summary>
    /// Information about the currently loaded WAD file.
    /// </summary>
    public WadFileInfo WadInfo
    {
        get => _wadInfo;
        set => this.RaiseAndSetIfChanged(ref _wadInfo, value);
    }

    /// <summary>
    /// Content of the currently selected file.
    /// </summary>
    public WadFileContent FileContent
    {
        get => _fileContent;
        set => this.RaiseAndSetIfChanged(ref _fileContent, value);
    }

    /// <summary>
    /// Tree nodes representing the WAD file structure.
    /// </summary>
    public ObservableCollection<WadTreeNode> TreeNodes
    {
        get => _treeNodes;
        set => this.RaiseAndSetIfChanged(ref _treeNodes, value);
    }

    /// <summary>
    /// Currently selected tree node.
    /// </summary>
    public WadTreeNode? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    /// <summary>
    /// Whether a WAD loading operation is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Status text displayed in the UI.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    /// <summary>
    /// Whether a WAD file is currently loaded.
    /// </summary>
    public bool HasWadLoaded => _currentArchive != null;

    #endregion

    #region Commands

    /// <summary>
    /// Command to load a WAD file.
    /// </summary>
    public ICommand LoadWadCommand { get; }

    /// <summary>
    /// Command to refresh the current WAD file.
    /// </summary>
    public ICommand RefreshCommand { get; }

    #endregion

    #region Static Converters

    /// <summary>
    /// Converter for folder/file icons.
    /// </summary>
    public static readonly IValueConverter FolderIconConverter = new FuncValueConverter<bool, string>(
        isFolder => isFolder ? "\uE8B7" : "\uE8A5" // Folder and File icons
    );

    /// <summary>
    /// Converter for icon colors.
    /// </summary>
    public static readonly IValueConverter IconColorConverter = new FuncValueConverter<bool, IBrush>(
        isFolder => isFolder ? 
            new SolidColorBrush(Color.FromRgb(255, 206, 84)) : // Folder color
            new SolidColorBrush(Color.FromRgb(220, 220, 220))   // File color
    );

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads a WAD file from the specified path.
    /// </summary>
    public async Task<bool> LoadWadFromPathAsync(string filePath)
    {
        try
        {
            IsLoading = true;
            StatusText = "Loading WAD file...";

            var (archive, stream) = await WadViewerService.LoadArchiveFromFileAsync(filePath);
            if (archive == null || stream == null)
            {
                StatusText = "Failed to load WAD file";
                return false;
            }

            await SetCurrentArchive(archive, stream, filePath);
            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading WAD: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles the Load WAD command.
    /// </summary>
    private async Task LoadWadFileAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Opening file picker...";

            var mainWindow = _mainViewModel?.GetMainWindow();
            if (mainWindow == null)
            {
                StatusText = "Error: Cannot find main window";
                MessageService.Error("Cannot find main window for file dialog.")
                    .WithDuration(TimeSpan.FromSeconds(3))
                    .Send();
                return;
            }

            var (archive, filePath) = await WadViewerService.LoadWadFileAsync(mainWindow);
            if (archive == null || string.IsNullOrEmpty(filePath))
            {
                StatusText = HasWadLoaded ? $"Loaded: {WadInfo.FileName}" : "No WAD file loaded";
                return;
            }

            // Load the archive with stream management
            var (loadedArchive, stream) = await WadViewerService.LoadArchiveFromFileAsync(filePath);
            if (loadedArchive == null || stream == null)
            {
                StatusText = "Failed to load WAD file";
                return;
            }

            await SetCurrentArchive(loadedArchive, stream, filePath);
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            MessageService.Error($"Failed to load WAD file: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the currently loaded WAD file.
    /// </summary>
    private async Task RefreshCurrentWadAsync()
    {
        if (string.IsNullOrEmpty(WadInfo.FilePath))
            return;

        await LoadWadFromPathAsync(WadInfo.FilePath);
    }

    /// <summary>
    /// Sets the current archive and updates the UI.
    /// </summary>
    private async Task SetCurrentArchive(Archive archive, Stream stream, string filePath)
    {
        // Dispose previous stream if exists
        _archiveStream?.Dispose();
        
        _currentArchive = archive;
        _archiveStream = stream;

        // Update WAD info
        WadInfo.FileName = Path.GetFileName(filePath);
        WadInfo.FilePath = filePath;
        WadInfo.TotalFiles = archive.FileCount;
        WadInfo.TotalSize = new FileInfo(filePath).Length;

        // Extract and cache all file data immediately to avoid stream disposal issues
        await Task.Run(() => ExtractAllFileData());

        // Build tree structure
        await Task.Run(() => BuildFileTree());

        // Clear current file content
        FileContent = new WadFileContent();

        StatusText = $"Loaded: {WadInfo.FileName} ({WadInfo.TotalFiles} files)";

        // Raise property changed for HasWadLoaded
        this.RaisePropertyChanged(nameof(HasWadLoaded));
    }

    /// <summary>
    /// Extracts all file data from the archive and caches it.
    /// </summary>
    private void ExtractAllFileData()
    {
        if (_currentArchive == null)
            return;

        _fileDataCache.Clear();

        foreach (var file in _currentArchive.Files)
        {
            var fileName = file.Key;
            try
            {
                var fileMemory = _currentArchive.OpenFile(fileName);
                if (fileMemory != null)
                {
                    // Extract the data immediately while the stream is still valid
                    _fileDataCache[fileName] = fileMemory.Value.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract file {fileName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Builds the tree structure from the WAD file list.
    /// </summary>
    private void BuildFileTree()
    {
        if (_currentArchive == null)
            return;

        var root = new Dictionary<string, WadTreeNode>();
        var allNodes = new Dictionary<string, WadTreeNode>();

        // Process each file in the archive
        foreach (var file in _currentArchive.Files)
        {
            var fileName = file.Key;
            var fileEntry = file.Value.Value;
            var pathParts = fileName.Split('/', StringSplitOptions.RemoveEmptyEntries);

            WadTreeNode? currentParent = null;
            var currentPath = "";

            // Create folder hierarchy
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                var part = pathParts[i];
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

                if (!allNodes.TryGetValue(currentPath, out var folderNode))
                {
                    folderNode = new WadTreeNode(part, WadTreeNodeType.Folder, currentPath);
                    allNodes[currentPath] = folderNode;

                    if (currentParent == null)
                    {
                        root[part] = folderNode;
                    }
                    else
                    {
                        currentParent.Children.Add(folderNode);
                    }
                }

                currentParent = folderNode;
            }

            // Create file node
            var fileNodeName = pathParts[^1];
            var fileNode = new WadTreeNode(fileNodeName, WadTreeNodeType.File, fileName)
            {
                FileSizeBytes = fileEntry.UncompressedSize
            };

            if (currentParent == null)
            {
                root[fileNodeName] = fileNode;
            }
            else
            {
                currentParent.Children.Add(fileNode);
            }
        }

        // Update TreeNodes on UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TreeNodes.Clear();
            foreach (var rootNode in root.Values.OrderBy(n => n.IsFile).ThenBy(n => n.Name))
            {
                TreeNodes.Add(rootNode);
            }
        });
    }

    /// <summary>
    /// Handles when a tree node is selected.
    /// </summary>
    private async Task OnNodeSelectionChanged(WadTreeNode? node)
    {
        if (node == null || !node.IsFile)
        {
            FileContent = new WadFileContent();
            return;
        }

        try
        {
            StatusText = $"Loading file: {node.Name}";

            // Get cached file data
            if (!_fileDataCache.TryGetValue(node.FullPath, out var fileData))
            {
                FileContent = new WadFileContent
                {
                    FileName = node.Name,
                    Content = "File data not found in cache"
                };
                return;
            }

            var (content, isDeserialized) = await Task.Run(() => 
                WadViewerService.GetFileContentFromData(node.FullPath, fileData));

            FileContent = new WadFileContent
            {
                FileName = node.Name,
                FileExtension = node.FileExtension,
                FileSizeBytes = node.FileSizeBytes,
                Content = content,
                IsDeserialized = isDeserialized
            };

            StatusText = $"Loaded: {WadInfo.FileName} ({WadInfo.TotalFiles} files) | Viewing: {node.Name}";
        }
        catch (Exception ex)
        {
            FileContent = new WadFileContent
            {
                FileName = node.Name,
                Content = $"Error loading file: {ex.Message}"
            };

            StatusText = $"Error loading file: {ex.Message}";
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the ViewModel and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _archiveStream?.Dispose();
        _archiveStream = null;
        _currentArchive = null;
        _fileDataCache.Clear();
    }

    #endregion
}