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
using Imcodec.Types;
using Imview.Core.Common.Constants;
using Imview.Core.Controls.Base;
using Imview.Core.Services;
using System;

namespace Imview.Core.Controls.Results;

public class ResAddDynaModEditor : EditorWindowBase<ResAddDynaMod> {
    
    private readonly ResAddDynaMod _result;
    private readonly TextBox _clientTagInput;
    private readonly CheckBox _removeCheckBox;
    private readonly CheckBox _useQuestAsOriginatorCheckBox;
    private readonly TextBox _stateInput;
    private readonly TextBox _zoneNameInput;

    public ResAddDynaModEditor(ResAddDynaMod? result = null) 
        : base(result is not null ? "Edit Dynamic Modifier" : "Add Dynamic Modifier") {
        
        _result = result ?? new ResAddDynaMod();
        
        Width = EditorConstants.DEFAULT_WINDOW_WIDTH;
        Height = 500;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _clientTagInput = new TextBox {
            Text = _result.m_dynaModClientTag.ToString(),
            Watermark = "Dynamic modifier client tag"
        };
        
        _removeCheckBox = new CheckBox {
            IsChecked = _result.m_dynaModRemove,
            Content = "Remove dynamic modifier"
        };
        
        _useQuestAsOriginatorCheckBox = new CheckBox {
            IsChecked = _result.m_useQuestAsOriginator,
            Content = "Use quest as originator"
        };
        
        _stateInput = new TextBox {
            Text = _result.m_dynaModState.ToString(),
            Watermark = "Dynamic modifier state"
        };
        
        _zoneNameInput = new TextBox {
            Text = _result.m_zoneName.ToString(),
            Watermark = "Zone name (optional)"
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        var clientTagPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Client Tag:" },
                _clientTagInput,
                new TextBlock { 
                    Text = "Identifier for the dynamic modifier on the client", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var statePanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "State:" },
                _stateInput,
                new TextBlock { 
                    Text = "State or value for the dynamic modifier", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var optionsPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                _removeCheckBox,
                _useQuestAsOriginatorCheckBox
            }
        };
        
        var zonePanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Zone Name:" },
                _zoneNameInput,
                new TextBlock { 
                    Text = "Specific zone where this modifier applies (optional)", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Dynamic Modifier Settings", clientTagPanel));
        MainPanel.Children.Add(CreateGroupBox("State", statePanel));
        MainPanel.Children.Add(CreateGroupBox("Options", optionsPanel));
        MainPanel.Children.Add(CreateGroupBox("Zone Settings", zonePanel));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }
    
    private void Save() {
        try {
            if (string.IsNullOrWhiteSpace(_clientTagInput.Text)) {
                MessageService
                    .Error("Client tag is required.")
                    .Send();
                return;
            }
            
            _result.m_dynaModClientTag = _clientTagInput.Text;
            _result.m_dynaModRemove = _removeCheckBox.IsChecked ?? false;
            _result.m_useQuestAsOriginator = _useQuestAsOriginatorCheckBox.IsChecked ?? false;
            _result.m_dynaModState = _stateInput.Text ?? string.Empty;
            _result.m_zoneName = _zoneNameInput.Text ?? string.Empty;
            
            ResultSource.SetResult(_result);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save dynamic modifier: {ex.Message}")
                .Send();
        }
    }
}