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
using System.Windows.Input;
using Imview.Core.ViewModels;
using ReactiveUI;
using Avalonia.Notification;
using Imview.Core.Services;
using Avalonia.Controls;
using System.Collections.Generic;
using Imcodec.ObjectProperty.TypeCache;
using System.Threading.Tasks;

namespace Imview.Core.ViewModels;

public class MainWindowViewModel : ViewModelBase {

    public INotificationMessageManager Manager { get; } = new NotificationMessageManager();
    public ICommand CreateQuestCommand { get; }
    public ICommand LoadQuestCommand { get; }

    private ViewModelBase _currentViewModel;
    private Avalonia.Controls.Window? _mainWindow;

    public MainWindowViewModel() {
        _currentViewModel = new SplashPageViewModel(this);
        CreateQuestCommand = ReactiveCommand.Create(CreateNewQuest);
        LoadQuestCommand = ReactiveCommand.Create(LoadQuest);

        MessageService.Initialize(Manager);
    }

    public void Initialize(Avalonia.Controls.Window window) {
        _mainWindow = window;
    }

    public ViewModelBase CurrentViewModel {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public void CreateNewQuest() {
        CurrentViewModel = new QuestTemplateEditorViewModel(this);
    }

    public async void LoadQuest() {
        try {
            if (_mainWindow == null) {
                MessageService.Error("Main window is not initialized.")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .Send();

                return;
            }

            var template = await TemplateSerializer.LoadTemplateAsync(_mainWindow);
            if (template != null) {
                CurrentViewModel = new QuestTemplateEditorViewModel(this, template);
                MessageService.Info("Quest template loaded successfully!")
                    .WithDuration(TimeSpan.FromSeconds(3))
                    .Send();
            }
        }
        catch (Exception ex) {
            MessageService.Error($"Failed to load quest template: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
    }

    public async void GetQuestsFromPacketCapture() {
        try {
            if (_mainWindow == null) {
                MessageService.Error("Main window is not initialized.")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .Send();

                return;
            }

            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions {
                Title = "Select Packet Capture File",
                AllowMultiple = false,
                FileTypeFilter = [
                new("JSON Packet Capture") {
                    Patterns = ["*.json"]
                },
                new("All Files") {
                    Patterns = ["*.*"]
                }
            ]
            };

            var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(options);
            if (files == null || files.Count == 0) {
                // User cancelled.
                return;
            }

            var filePath = files[0].Path.LocalPath;

            MessageService.Info("Reading packet capture, please wait...")
                .WithDuration(TimeSpan.FromSeconds(3))
                .Send();

            var questTemplates = await QuestPacketReaderService.ReadQuestsFromPacketCaptureAsync(filePath);

            // Switch to the packet view with the extracted quest templates.
            CurrentViewModel = new PacketQuestViewModel(this, filePath, questTemplates);
        }
        catch (Exception ex) {
            MessageService.Error($"Failed to load packet capture: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
    }

    /// <summary>
    /// Opens a quest template editor in a new window.
    /// </summary>
    /// <param name="template">The quest template to edit</param>
    /// <returns>A task that completes when the editor is closed</returns>
    public async Task OpenQuestEditorInNewWindow(QuestTemplate template) {
        try {
            if (_mainWindow == null) {
                MessageService.Error("Main window is not initialized.")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .Send();

                return;
            }

            // Create the editor window in read-only mode.
            var editorWindow = new Views.QuestEditorWindow(template, isReadOnly: true);
            await editorWindow.ShowDialog(_mainWindow);
        }
        catch (Exception ex) {
            MessageService.Error($"Error opening quest editor: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
    }

    /// <summary>
    /// Unpacks a KIWAD file, with optional deserialization.
    /// </summary>
    /// <param name="deserialize">Whether to attempt to deserialize files with supported extensions</param>
    public async void UnpackKiwad(bool deserialize) {
        try {
            if (_mainWindow == null) {
                MessageService.Error("Main window is not initialized.")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .Send();
                    
                return;
            }

            var result = await KIWadFileService.UnpackKiwadArchiveAsync(_mainWindow, deserialize);
            if (result) {
                MessageService.Info($"KIWAD archive unpacked successfully{(deserialize ? " with deserialization" : "")}.")
                    .WithDuration(TimeSpan.FromSeconds(3))
                    .Send();
            }
        }
        catch (Exception ex) {
            MessageService.Error($"Failed to unpack KIWAD: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
    }

    public void AnalyzeObjectPropertyBlob()
        => CurrentViewModel = new ObjectPropertyBlobViewModel(this);

    /// <summary>
    /// Returns the main window instance.
    /// </summary>
    public Avalonia.Controls.Window? GetMainWindow()
        => _mainWindow;

    public void ReturnToSplash()
        => CurrentViewModel = new SplashPageViewModel(this);

}