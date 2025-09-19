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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.Common.Constants;
using Imview.Core.Controls.Base;
using Imview.Core.Controls.Templates;
using Imview.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Imview.Core.Controls.Results;

public class ResActorDialogEditor : EditorWindowBase<ResActorDialog> {
    
    private readonly ResActorDialog _result;
    private readonly TextBox _activePersonaInput;
    private readonly TextBox _registryEntryInput;
    private readonly TextBox _questInput;
    private readonly CheckBox _broadcastToZoneCheckBox;
    private readonly CheckBox _displayInQuestListCheckBox;
    private readonly CheckBox _oneShotCheckBox;
    private readonly Avalonia.Controls.Button _editDialogButton;
    private ActorDialog _dialogCopy;

    public ResActorDialogEditor(ResActorDialog? result = null) 
        : base(result is not null ? "Edit Actor Dialog" : "Add Actor Dialog") {
        
        _result = result ?? new ResActorDialog();
        _dialogCopy = _result.m_dialog ?? new ActorDialog();
        
        Width = EditorConstants.DEFAULT_WINDOW_WIDTH;
        Height = 600;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _activePersonaInput = new TextBox {
            Text = _result.m_activePersona.ToString(),
            Watermark = "Active persona name"
        };
        
        _registryEntryInput = new TextBox {
            Text = _result.m_registryEntry.ToString(),
            Watermark = "Registry entry identifier"
        };
        
        _questInput = new TextBox {
            Text = _result.m_quest.ToString(),
            Watermark = "Quest name/identifier"
        };
        
        _broadcastToZoneCheckBox = new CheckBox {
            IsChecked = _result.m_broadcastToZone,
            Content = "Broadcast to zone"
        };
        
        _displayInQuestListCheckBox = new CheckBox {
            IsChecked = _result.m_displayInQuestList,
            Content = "Display in quest list"
        };
        
        _oneShotCheckBox = new CheckBox {
            IsChecked = _result.m_oneShot,
            Content = "One shot dialog"
        };
        
        _editDialogButton = new Avalonia.Controls.Button {
            Content = "Edit Dialog Content",
            Command = ReactiveCommand.CreateFromTask(EditDialog),
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 150
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        var personaPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Active Persona:" },
                _activePersonaInput,
                new TextBlock { 
                    Text = "The persona/character that will deliver this dialog", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var registryPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Registry Entry:" },
                _registryEntryInput,
                new TextBlock { 
                    Text = "Unique identifier for this dialog entry in the registry", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var questPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Quest:" },
                _questInput,
                new TextBlock { 
                    Text = "Associated quest name or identifier", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var optionsPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                _broadcastToZoneCheckBox,
                new TextBlock { 
                    Text = "When enabled, dialog is broadcast to all players in the zone", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Thickness(20, 0, 0, 10)
                },
                _displayInQuestListCheckBox,
                new TextBlock { 
                    Text = "When enabled, dialog appears in the player's quest list", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Thickness(20, 0, 0, 10)
                },
                _oneShotCheckBox,
                new TextBlock { 
                    Text = "When enabled, dialog can only be triggered once", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Thickness(20, 0, 0, 0)
                }
            }
        };
        
        var dialogPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { 
                    Text = "Dialog Content:",
                    FontWeight = Avalonia.Media.FontWeight.Bold
                },
                _editDialogButton,
                new TextBlock { 
                    Text = "Click the button above to open the dialog editor and configure the conversation", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Persona Settings", personaPanel));
        MainPanel.Children.Add(CreateGroupBox("Registry Settings", registryPanel));
        MainPanel.Children.Add(CreateGroupBox("Quest Association", questPanel));
        MainPanel.Children.Add(CreateGroupBox("Dialog Options", optionsPanel));
        MainPanel.Children.Add(CreateGroupBox("Dialog Content", dialogPanel));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }
    
    private async Task EditDialog() {
        try {
            // Create a temporary ActorDialogList to work with the existing dialog editor
            var dialogList = new ActorDialogList();
            
            // Add the current dialog to the list if it has content
            if (_dialogCopy.m_dialogEntries?.Count > 0) {
                dialogList.m_dialogs = new List<ActorDialog> { _dialogCopy };
            }
            
            // Create and configure the dialog editor
            var dialogEditor = new QuestDialogEditor {
                DialogList = dialogList,
                QuestTitle = _questInput.Text ?? "Actor Dialog"
            };
            
            // Create a window to host the dialog editor
            var editorWindow = new Avalonia.Controls.Window {
                Title = "Edit Actor Dialog Content",
                Width = 800,
                Height = 600,
                Content = dialogEditor,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            await editorWindow.ShowDialog(this);
            
            // Extract the updated dialog from the editor
            var updatedDialogList = dialogEditor.ToActorDialogList();
            if (updatedDialogList?.m_dialogs?.Count > 0) {
                _dialogCopy = updatedDialogList.m_dialogs[0];
            }
            
            MessageService
                .Info("Dialog content updated successfully.")
                .Send();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to edit dialog: {ex.Message}")
                .Send();
        }
    }
    
    private void Save() {
        try {
            _result.m_activePersona = _activePersonaInput.Text ?? string.Empty;
            _result.m_registryEntry = _registryEntryInput.Text ?? string.Empty;
            _result.m_quest = _questInput.Text ?? string.Empty;
            _result.m_broadcastToZone = _broadcastToZoneCheckBox.IsChecked ?? false;
            _result.m_displayInQuestList = _displayInQuestListCheckBox.IsChecked ?? false;
            _result.m_oneShot = _oneShotCheckBox.IsChecked ?? false;
            _result.m_dialog = _dialogCopy;
            
            ResultSource.SetResult(_result);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save actor dialog: {ex.Message}")
                .Send();
        }
    }
}