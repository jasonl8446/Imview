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

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Imview.Core.Services;
using Imview.Core.Models;

namespace Imview.Core.ViewModels;

public class ClientFileDownloadViewModel : ViewModelBase, IDisposable {

    private readonly ClientFileService _clientFileService;
    private string _selectedRevision;
    private bool _isLoadingFiles;
    private bool _isDownloading;
    private string _downloadStatus;
    private string _searchFilter;
    
    public ClientFileDownloadViewModel() {
        _clientFileService = new ClientFileService();
        _selectedRevision = _clientFileService.GetSelectedRevision();
        _downloadStatus = "Ready";
        _searchFilter = string.Empty;
        
        AvailableFiles = new ObservableCollection<FileRecordViewModel>();
        FilteredFiles = new ObservableCollection<FileRecordViewModel>();
        DownloadProgress = new ObservableCollection<DownloadProgress>();
        
        LoadFilesCommand = ReactiveCommand.CreateFromTask(LoadFilesAsync);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
        CloseCommand = ReactiveCommand.Create(Close);
        ClearCacheCommand = ReactiveCommand.Create(ClearCache);
        
        var canDownload = this.WhenAnyValue(
            x => x.IsDownloading,
            x => x.FilteredFiles.Count,
            (downloading, fileCount) => !downloading && fileCount > 0);
            
        DownloadSelectedCommand = ReactiveCommand.CreateFromTask(DownloadSelectedFilesAsync, canDownload);
        
        this.WhenAnyValue(x => x.SearchFilter)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => FilterFiles());
            
        LoadFilesAsync().ConfigureAwait(false);
    }
    
    public string SelectedRevision {
        get => _selectedRevision;
        set => this.RaiseAndSetIfChanged(ref _selectedRevision, value);
    }
    
    public bool IsLoadingFiles {
        get => _isLoadingFiles;
        private set => this.RaiseAndSetIfChanged(ref _isLoadingFiles, value);
    }
    
    public bool IsDownloading {
        get => _isDownloading;
        private set => this.RaiseAndSetIfChanged(ref _isDownloading, value);
    }
    
    public string DownloadStatus {
        get => _downloadStatus;
        private set => this.RaiseAndSetIfChanged(ref _downloadStatus, value);
    }
    
    public string SearchFilter {
        get => _searchFilter;
        set => this.RaiseAndSetIfChanged(ref _searchFilter, value);
    }
    
    public ObservableCollection<FileRecordViewModel> AvailableFiles { get; }
    public ObservableCollection<FileRecordViewModel> FilteredFiles { get; }
    public ObservableCollection<DownloadProgress> DownloadProgress { get; }
    
    public ICommand LoadFilesCommand { get; }
    public ICommand DownloadSelectedCommand { get; private set; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ClearCacheCommand { get; }
    
    public event EventHandler? CloseRequested;
    
    private async Task LoadFilesAsync() {
        if (string.IsNullOrEmpty(SelectedRevision)) {
            DownloadStatus = "No revision selected";
            return;
        }
        
        IsLoadingFiles = true;
        DownloadStatus = "Loading file list...";
        
        try {
            var fileList = await _clientFileService.FetchFileListAsync(SelectedRevision);
            
            AvailableFiles.Clear();
            
            // Sort files with Root.wad at the top, then alphabetically
            var sortedRecords = fileList.Records
                .OrderBy(r => r.DisplayName.Equals("Root", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
                
            foreach (var record in sortedRecords) {
                var viewModel = new FileRecordViewModel(record, _clientFileService.IsFileCached(record, SelectedRevision));
                AvailableFiles.Add(viewModel);
            }
            
            FilterFiles();
            DownloadStatus = $"Loaded {AvailableFiles.Count} files";
        }
        catch (Exception ex) {
            DownloadStatus = $"Error loading files: {ex.Message}";
        }
        finally {
            IsLoadingFiles = false;
        }
    }
    
    private void FilterFiles() {
        FilteredFiles.Clear();
        
        var filtered = string.IsNullOrWhiteSpace(SearchFilter)
            ? AvailableFiles
            : AvailableFiles.Where(f => f.FileRecord.DisplayName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
            
        foreach (var file in filtered) {
            FilteredFiles.Add(file);
        }
        
        // Force UI update
        this.RaisePropertyChanged(nameof(FilteredFiles));
    }
    
    private async Task DownloadSelectedFilesAsync() {
        var selectedFiles = FilteredFiles.Where(f => f.IsSelected).ToList();
        if (!selectedFiles.Any()) {
            DownloadStatus = "No files selected";
            return;
        }
        
        IsDownloading = true;
        DownloadProgress.Clear();
        
        try {
            var progress = new Progress<DownloadProgress>(p => {
                var existing = DownloadProgress.FirstOrDefault(dp => dp.FileName == p.FileName);
                if (existing != null) {
                    var index = DownloadProgress.IndexOf(existing);
                    DownloadProgress[index] = p;
                } else {
                    DownloadProgress.Add(p);
                }
            });
            
            var fileRecords = selectedFiles.Select(f => f.FileRecord);
            var downloadedFiles = await _clientFileService.DownloadFilesAsync(fileRecords, SelectedRevision, progress);
            
            DownloadStatus = $"Downloaded {downloadedFiles.Count} files successfully";
            
            foreach (var fileViewModel in selectedFiles) {
                fileViewModel.IsCached = _clientFileService.IsFileCached(fileViewModel.FileRecord, SelectedRevision);
            }
        }
        catch (Exception ex) {
            DownloadStatus = $"Download error: {ex.Message}";
        }
        finally {
            IsDownloading = false;
        }
    }
    
    private void SelectAll() {
        foreach (var file in FilteredFiles) {
            file.IsSelected = true;
        }
    }
    
    private void DeselectAll() {
        foreach (var file in FilteredFiles) {
            file.IsSelected = false;
        }
    }
    
    private void Close() {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
    private void ClearCache() {
        _clientFileService.ClearCache();
        DownloadStatus = "Cache cleared";
        
        foreach (var file in AvailableFiles) {
            file.IsCached = false;
        }
    }
    
    public void Dispose() {
        _clientFileService?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class FileRecordViewModel : ViewModelBase {
    private bool _isSelected;
    private bool _isCached;
    
    public FileRecordViewModel(FileRecord fileRecord, bool isCached) {
        FileRecord = fileRecord;
        _isCached = isCached;
    }
    
    public FileRecord FileRecord { get; }
    
    public bool IsSelected {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
    
    public bool IsCached {
        get => _isCached;
        set => this.RaiseAndSetIfChanged(ref _isCached, value);
    }
    
    public string CacheStatus => IsCached ? "Cached" : "Not Cached";
}