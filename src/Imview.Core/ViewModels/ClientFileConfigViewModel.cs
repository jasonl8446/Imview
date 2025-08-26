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
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Imview.Core.Services;

namespace Imview.Core.ViewModels;

public class ClientFileConfigViewModel : ViewModelBase, IDisposable {

    private readonly ClientFileService _clientFileService;
    private string _revisionsUrl;
    private string _selectedRevision;
    private bool _isTestingConnection;
    private bool _isLoadingRevisions;
    private string _connectionStatus;
    
    public ClientFileConfigViewModel() {
        _clientFileService = new ClientFileService();
        _revisionsUrl = _clientFileService.GetRevisionsUrl();
        _selectedRevision = _clientFileService.GetSelectedRevision();
        _connectionStatus = "Not tested";
        
        Revisions = new ObservableCollection<string>();
        
        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        LoadRevisionsCommand = ReactiveCommand.CreateFromTask(LoadRevisionsAsync);
        SaveCommand = ReactiveCommand.Create(Save);
        CancelCommand = ReactiveCommand.Create(Cancel);
        
        var canLoadRevisions = this.WhenAnyValue(x => x.IsTestingConnection, x => x.IsLoadingRevisions)
            .Select(x => !x.Item1 && !x.Item2);
            
        LoadRevisionsCommand = ReactiveCommand.CreateFromTask(LoadRevisionsAsync, canLoadRevisions);
        
        LoadRevisionsAsync().ConfigureAwait(false);
    }
    
    public string RevisionsUrl {
        get => _revisionsUrl;
        set => this.RaiseAndSetIfChanged(ref _revisionsUrl, value);
    }
    
    public string SelectedRevision {
        get => _selectedRevision;
        set => this.RaiseAndSetIfChanged(ref _selectedRevision, value);
    }
    
    public bool IsTestingConnection {
        get => _isTestingConnection;
        private set => this.RaiseAndSetIfChanged(ref _isTestingConnection, value);
    }
    
    public bool IsLoadingRevisions {
        get => _isLoadingRevisions;
        private set => this.RaiseAndSetIfChanged(ref _isLoadingRevisions, value);
    }
    
    public string ConnectionStatus {
        get => _connectionStatus;
        private set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }
    
    public ObservableCollection<string> Revisions { get; }
    
    public ICommand TestConnectionCommand { get; }
    public ICommand LoadRevisionsCommand { get; private set; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    public event EventHandler<bool>? DialogResult;
    
    private async Task TestConnectionAsync() {
        IsTestingConnection = true;
        ConnectionStatus = "Testing...";
        
        try {
            _clientFileService.SetRevisionsUrl(RevisionsUrl);
            var success = await _clientFileService.TestConnectionAsync();
            ConnectionStatus = success ? "Connection successful" : "Connection failed";
            
            if (success) {
                await LoadRevisionsAsync();
            }
        }
        catch (Exception ex) {
            ConnectionStatus = $"Error: {ex.Message}";
        }
        finally {
            IsTestingConnection = false;
        }
    }
    
    private async Task LoadRevisionsAsync() {
        if (IsLoadingRevisions) return;
        
        IsLoadingRevisions = true;
        
        try {
            _clientFileService.SetRevisionsUrl(RevisionsUrl);
            var revisions = await _clientFileService.FetchRevisionsAsync();
            
            Revisions.Clear();
            foreach (var revision in revisions) {
                Revisions.Add(revision);
            }
            
            if (!string.IsNullOrEmpty(SelectedRevision) && !Revisions.Contains(SelectedRevision)) {
                SelectedRevision = Revisions.Count > 0 ? Revisions[0] : string.Empty;
            }
        }
        catch (Exception ex) {
            ConnectionStatus = $"Error loading revisions: {ex.Message}";
        }
        finally {
            IsLoadingRevisions = false;
        }
    }
    
    private void Save() {
        _clientFileService.SetRevisionsUrl(RevisionsUrl);
        _clientFileService.SetSelectedRevision(SelectedRevision);
        DialogResult?.Invoke(this, true);
    }
    
    private void Cancel() {
        DialogResult?.Invoke(this, false);
    }
    
    public void Dispose() {
        _clientFileService?.Dispose();
        GC.SuppressFinalize(this);
    }

}