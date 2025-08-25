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
using System.Collections.ObjectModel;
using System.Windows.Input;
using Imcodec.ObjectProperty.TypeCache;
using ReactiveUI;
using System.Linq;
using Imview.Core.Services;

namespace Imview.Core.ViewModels;

public class PacketQuestViewModel : ViewModelBase {

    private readonly MainWindowViewModel _mainViewModel;
    private QuestTemplate? _selectedQuest;
    private string _sourceFilePath;

    public ObservableCollection<QuestTemplate> QuestTemplates { get; }
    public ICommand BackToSplashCommand { get; }
    public ICommand EditSelectedQuestCommand { get; }
    public ICommand SaveAllQuestsCommand { get; }

    public bool HasQuests => QuestTemplates != null && QuestTemplates.Count > 0;
    public bool HasSelectedQuest => SelectedQuest != null;

    public string SourceFilePath {
        get => _sourceFilePath;
        set => this.RaiseAndSetIfChanged(ref _sourceFilePath, value);
    }

    public QuestTemplate? SelectedQuest {
        get => _selectedQuest;
        set {
            this.RaiseAndSetIfChanged(ref _selectedQuest, value);
            this.RaisePropertyChanged(nameof(HasSelectedQuest));
        }
    }

    public PacketQuestViewModel(MainWindowViewModel mainViewModel, string filePath, ObservableCollection<QuestTemplate> questTemplates) {
        _mainViewModel = mainViewModel;
        _sourceFilePath = filePath;
        QuestTemplates = questTemplates ?? [];

        BackToSplashCommand = ReactiveCommand.Create(BackToSplash);
        EditSelectedQuestCommand = ReactiveCommand.Create(EditSelectedQuest);
        SaveAllQuestsCommand = ReactiveCommand.Create(SaveAllQuests);

        // Update the HasQuests property
        this.RaisePropertyChanged(nameof(HasQuests));

        if (HasQuests) {
            MessageService.Info($"Found {QuestTemplates.Count} quests in the packet capture.")
                .WithDuration(TimeSpan.FromSeconds(3))
                .Send();
        }
    }

    private void EditSelectedQuest() {
        if (SelectedQuest is not null) {
            _mainViewModel.OpenQuestEditorInNewTab(SelectedQuest);
        }
    }

    private async void SaveAllQuests() {
        try {
            if (!HasQuests) {
                MessageService.Error("No quests to save.")
                    .WithDuration(TimeSpan.FromSeconds(3))
                    .Send();

                return;
            }
            
            var window = _mainViewModel.GetMainWindow();
            if (window == null) {
                MessageService.Error("Cannot find main window.")
                    .WithDuration(TimeSpan.FromSeconds(3))
                    .Send();
                    
                return;
            }
            
            MessageService.Info("Preparing to save quests...")
                .WithDuration(TimeSpan.FromSeconds(3))
                .Send();
                
            var success = await QuestZipService.SaveQuestsToZipFileAsync(QuestTemplates, window);
            
            if (success) {
                MessageService.Info($"Successfully saved {QuestTemplates.Count} quests to zip file.")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .Send();
            }
        }
        catch (Exception ex) {
            MessageService.Error($"Failed to save quests: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
    }

    private void BackToSplash() 
        => _mainViewModel.ReturnToSplash();

}