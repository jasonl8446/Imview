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
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Imview.Core.Database.Collections;
using Imview.Core.Services;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace Imview.Core.ViewModels;

public class QuestBrowserViewModel : ViewModelBase {
    
    private readonly ObservableCollection<QuestDocument> _quests = new();
    private readonly ObservableCollection<QuestDocument> _filteredQuests = new();
    private QuestDocument? _selectedQuest;
    private string _searchText = "";
    private bool _isLoading = false;
    private string _statusText = "";
    private bool _isEditing = false;

    public QuestBrowserViewModel() {
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        NewQuestCommand = ReactiveCommand.Create(NewQuest);
        DeleteSelectedCommand = ReactiveCommand.CreateFromTask(DeleteSelectedAsync, 
            this.WhenAnyValue(x => x.HasSelectedQuest));
        EditCommand = ReactiveCommand.Create(ToggleEdit);
        OpenEditorCommand = ReactiveCommand.Create(OpenEditor, 
            this.WhenAnyValue(x => x.HasSelectedQuest));
        ExportCommand = ReactiveCommand.CreateFromTask(ExportAsync, 
            this.WhenAnyValue(x => x.HasSelectedQuest));

        // Set up search filtering
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => FilterQuests());

        // Load quests on initialization
        _ = RefreshAsync();
    }

    public ObservableCollection<QuestDocument> FilteredQuests => _filteredQuests;

    public QuestDocument? SelectedQuest {
        get => _selectedQuest;
        set => this.RaiseAndSetIfChanged(ref _selectedQuest, value);
    }

    public string SearchText {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public bool IsLoading {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string StatusText {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public bool IsEditing {
        get => _isEditing;
        set => this.RaiseAndSetIfChanged(ref _isEditing, value);
    }

    public bool HasSelectedQuest => SelectedQuest != null;

    public string QuestCountText => $"Quests ({_filteredQuests.Count})";

    public string EditButtonText => IsEditing ? "Save Changes" : "Edit";

    public string SelectedQuestPreview {
        get {
            if (SelectedQuest?.Template == null) return "";
            try {
                return JsonSerializer.Serialize(SelectedQuest.Template, new JsonSerializerOptions { 
                    WriteIndented = true 
                });
            }
            catch {
                return "Unable to preview template";
            }
        }
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> NewQuestCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> EditCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenEditorCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }

    private async Task RefreshAsync() {
        try {
            IsLoading = true;
            StatusText = "Loading quests...";

            var quests = await QuestCollection.GetAllQuestsAsync();
            
            _quests.Clear();
            foreach (var quest in quests) {
                _quests.Add(quest);
            }

            FilterQuests();
            StatusText = $"Loaded {quests.Count} quest(s)";
        }
        catch (Exception ex) {
            StatusText = $"Error loading quests: {ex.Message}";
        }
        finally {
            IsLoading = false;
        }
    }

    private void FilterQuests() {
        _filteredQuests.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText) 
            ? _quests
            : _quests.Where(q => 
                (q.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (q.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

        foreach (var quest in filtered) {
            _filteredQuests.Add(quest);
        }

        this.RaisePropertyChanged(nameof(QuestCountText));
    }

    private void NewQuest() {
        // This would typically open the quest editor with a new quest
        // For now, just show a message
        StatusText = "New quest functionality would open quest editor";
    }

    private async Task DeleteSelectedAsync() {
        if (SelectedQuest == null) return;

        try {
            var success = await QuestCollection.DeleteQuestAsync(SelectedQuest.Id);
            if (success) {
                StatusText = $"Deleted quest '{SelectedQuest.Name}'";
                await RefreshAsync();
                SelectedQuest = null;
            } else {
                StatusText = "Failed to delete quest";
            }
        }
        catch (Exception ex) {
            StatusText = $"Error deleting quest: {ex.Message}";
        }
    }

    private async void ToggleEdit() {
        if (!IsEditing) {
            IsEditing = true;
        } else {
            // Save changes
            if (SelectedQuest != null) {
                try {
                    var success = await QuestCollection.UpdateQuestAsync(
                        SelectedQuest.Id, 
                        SelectedQuest.Template, 
                        SelectedQuest.Name, 
                        SelectedQuest.Description);
                    
                    if (success) {
                        StatusText = "Quest updated successfully";
                        await RefreshAsync();
                    } else {
                        StatusText = "Failed to update quest";
                    }
                } catch (Exception ex) {
                    StatusText = $"Error updating quest: {ex.Message}";
                }
            }
            IsEditing = false;
        }
    }

    private void OpenEditor() {
        if (SelectedQuest?.Template == null) return;
        
        // This would open the selected quest in the quest editor
        // For now, just show a message
        StatusText = $"Would open '{SelectedQuest.Name}' in quest editor";
    }

    private async Task ExportAsync() {
        if (SelectedQuest?.Template == null) return;

        try {
            // Get the main window for the file dialog
            var app = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var parentWindow = app?.MainWindow;
            
            if (parentWindow != null) {
                var success = await TemplateSerializer.SaveTemplateToFileAsync(SelectedQuest.Template, parentWindow);
                if (success) {
                    StatusText = $"Exported '{SelectedQuest.Name}' to file";
                } else {
                    StatusText = "Export cancelled or failed";
                }
            }
        }
        catch (Exception ex) {
            StatusText = $"Error exporting quest: {ex.Message}";
        }
    }
}