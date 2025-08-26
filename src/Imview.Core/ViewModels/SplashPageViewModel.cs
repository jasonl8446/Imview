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
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Linq;
using Imview.Core.Services;
using Imview.Core.Models;

namespace Imview.Core.ViewModels;

public class SplashPageViewModel : ViewModelBase {

    private readonly ClientFileService _clientFileService = new();
    private readonly MainWindowViewModel _mainViewModel;
    private bool _isDownloadingRootWad;
    private string _rootWadDownloadStatus = string.Empty;
    private bool _isTestingConnection = true;
    private bool _isConnectionAvailable;
    private bool _hasSelectedRevision;
    
    public string? RootWadWarning => GetRootWadWarning();
    public bool HasRootWadWarning => !string.IsNullOrEmpty(RootWadWarning);
    
    public bool IsTestingConnection {
        get => _isTestingConnection;
        private set => this.RaiseAndSetIfChanged(ref _isTestingConnection, value);
    }
    
    public bool IsConnectionAvailable {
        get => _isConnectionAvailable;
        private set {
            this.RaiseAndSetIfChanged(ref _isConnectionAvailable, value);
            this.RaisePropertyChanged(nameof(ShouldShowSettingsButton));
        }
    }
    
    public bool HasSelectedRevision {
        get => _hasSelectedRevision;
        private set {
            this.RaiseAndSetIfChanged(ref _hasSelectedRevision, value);
            this.RaisePropertyChanged(nameof(ShouldShowSettingsButton));
        }
    }
    
    public bool IsDownloadingRootWad {
        get => _isDownloadingRootWad;
        private set => this.RaiseAndSetIfChanged(ref _isDownloadingRootWad, value);
    }
    
    public string RootWadDownloadStatus {
        get => _rootWadDownloadStatus;
        private set => this.RaiseAndSetIfChanged(ref _rootWadDownloadStatus, value);
    }
    
    public ICommand DownloadRootWadCommand { get; }
    public ICommand OpenClientFileSettingsCommand { get; }
    
    public bool ShouldShowSettingsButton => !IsConnectionAvailable || !HasSelectedRevision;

    public SplashPageViewModel(MainWindowViewModel mainViewModel) {
        _mainViewModel = mainViewModel;
        DownloadRootWadCommand = ReactiveCommand.CreateFromTask(DownloadRootWadAsync);
        OpenClientFileSettingsCommand = ReactiveCommand.Create(() => _mainViewModel.ShowClientFileConfig());
        InitializeSections();
        
        // Test connection on startup
        _ = Task.Run(TestConnectionAsync);
    }

    public ObservableCollection<SplashSectionViewModel> Sections { get; private set; } = null!;

    private void InitializeSections() {
        Sections = [
            new SplashSectionViewModel(
                "Quests",
                "Quest Editor",
                "Get Quests From Packet Capture",
                "",
                "",
                _mainViewModel.ShowQuestBrowser,
                _mainViewModel.GetQuestsFromPacketCapture,
                null,
                null),
        new SplashSectionViewModel(
            "Files",
            "Unpack KIWADs",
            "Unpack KIWADs & Deserialize",
            "Download Client Files",
            "",
            () => _mainViewModel.UnpackKiwad(false),
            () => _mainViewModel.UnpackKiwad(true),
            _mainViewModel.ShowClientFileDownload,
            null),
        new SplashSectionViewModel(
            "Object Property",
            "Analyze Blob",
            "",
            "",
            "",
            _mainViewModel.AnalyzeObjectPropertyBlob,
            null,
            null,
            null),
        new SplashSectionViewModel(
            "Settings",
            "Configure Database",
            "Configure Client Files",
            "",
            "",
            _mainViewModel.ShowDatabaseConfig,
            _mainViewModel.ShowClientFileConfig,
            null,
            null)
        ];
    }
    
    private string? GetRootWadWarning() {
        // Still testing connection
        if (IsTestingConnection) {
            return null; // Don't show warning while testing
        }
        
        // No revision selected
        if (!HasSelectedRevision) {
            return "⚠️ No client revision selected. Please configure your client revision in 'Settings > Configure Client Files' to download Root.wad.";
        }
        
        // Connection not available
        if (!IsConnectionAvailable) {
            return "⚠️ Cannot connect to client file host. Please check your internet connection or update the host URL in 'Settings > Configure Client Files'.";
        }
        
        // Connection available but Root.wad not cached
        if (!_clientFileService.IsRootWadCached()) {
            return "⚠️ Root.wad is not cached. This critical file is required for game data analysis.";
        }
        
        // All good - no warning needed
        return null;
    }
    
    private async Task DownloadRootWadAsync() {
        try {
            var selectedRevision = _clientFileService.GetSelectedRevision();
            if (string.IsNullOrEmpty(selectedRevision)) {
                RootWadDownloadStatus = "Please configure and select a client revision first.";
                return;
            }
            
            IsDownloadingRootWad = true;
            RootWadDownloadStatus = "Fetching file list...";
            
            // Get the file list to find Root.wad
            var fileList = await _clientFileService.FetchFileListAsync(selectedRevision);
            var rootWadRecord = fileList.Records.FirstOrDefault(r => r.SourceFileName.Contains("Root.wad", StringComparison.OrdinalIgnoreCase));
            
            if (rootWadRecord == null) {
                RootWadDownloadStatus = "Root.wad not found in file list.";
                return;
            }
            
            RootWadDownloadStatus = "Downloading Root.wad...";
            
            var progress = new Progress<DownloadProgress>(p => {
                RootWadDownloadStatus = $"Downloading: {p.PercentComplete:F1}% ({p.BytesDownloaded}/{p.TotalBytes} bytes)";
            });
            
            var downloadedPath = await _clientFileService.DownloadFileAsync(rootWadRecord, selectedRevision, progress);
            
            RootWadDownloadStatus = "Root.wad downloaded successfully!";
            
            // Refresh the warning display
            this.RaisePropertyChanged(nameof(HasRootWadWarning));
            this.RaisePropertyChanged(nameof(RootWadWarning));
            
            // Clear the status after a delay
            await Task.Delay(3000);
            RootWadDownloadStatus = string.Empty;
        }
        catch (Exception ex) {
            RootWadDownloadStatus = $"Download failed: {ex.Message}";
        }
        finally {
            IsDownloadingRootWad = false;
        }
    }
    
    private async Task TestConnectionAsync() {
        try {
            IsTestingConnection = true;
            
            // Check if revision is selected
            var selectedRevision = _clientFileService.GetSelectedRevision();
            HasSelectedRevision = !string.IsNullOrEmpty(selectedRevision);
            
            // Test connection to the file host
            IsConnectionAvailable = await _clientFileService.TestConnectionAsync();
            
        }
        catch {
            IsConnectionAvailable = false;
        }
        finally {
            IsTestingConnection = false;
            
            // Refresh the warning display after testing
            this.RaisePropertyChanged(nameof(HasRootWadWarning));
            this.RaisePropertyChanged(nameof(RootWadWarning));
        }
    }
    
}

public class SplashSectionViewModel(
    string title,
    string firstButtonText,
    string secondButtonText,
    string thirdButtonText,
    string fourthButtonText,
    System.Action? firstButtonAction,
    System.Action? secontButtonAction,
    System.Action? thirdButtonAction,
    System.Action? fourthButtonAction) {

    public string Title { get; } = title;
    public string FirstButtonText { get; } = firstButtonText;
    public string SecondButtonText { get; } = secondButtonText;
    public string ThirdButtonText { get; } = thirdButtonText;
    public string FourthButtonText { get; } = fourthButtonText;
    public ICommand? FirstButtonCommand { get; }
        = firstButtonAction != null
            ? ReactiveCommand.Create(firstButtonAction)
            : null;
    public ICommand? SecondButtonCommand { get; }
        = secontButtonAction != null
            ? ReactiveCommand.Create(secontButtonAction)
            : null;
    public ICommand? ThirdButtonCommand { get; }
        = thirdButtonAction != null
            ? ReactiveCommand.Create(thirdButtonAction)
            : null;
    public ICommand? FourthButtonCommand { get; }
        = fourthButtonAction != null
            ? ReactiveCommand.Create(fourthButtonAction)
            : null;
    public bool HasSecondButton
        => !string.IsNullOrEmpty(SecondButtonText)
        && SecondButtonCommand != null;
    public bool HasThirdButton
        => !string.IsNullOrEmpty(ThirdButtonText)
        && ThirdButtonCommand != null;
    public bool HasFourthButton
        => !string.IsNullOrEmpty(FourthButtonText)
        && FourthButtonCommand != null;

    public bool IsQuestSection => Title == "Quests";

}