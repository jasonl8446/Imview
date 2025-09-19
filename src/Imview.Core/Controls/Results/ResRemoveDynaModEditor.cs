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

public class ResRemoveDynaModEditor : EditorWindowBase<ResRemoveDynaMod> {
    
    private readonly ResRemoveDynaMod _result;
    private readonly TextBox _clientTagInput;
    private readonly CheckBox _useQuestAsOriginatorCheckBox;

    public ResRemoveDynaModEditor(ResRemoveDynaMod? result = null) 
        : base(result is not null ? "Edit Remove Dynamic Modifier" : "Remove Dynamic Modifier") {
        
        _result = result ?? new ResRemoveDynaMod();
        
        Width = EditorConstants.SMALL_WINDOW_WIDTH;
        Height = 300;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _clientTagInput = new TextBox {
            Text = _result.m_dynaModClientTag.ToString(),
            Watermark = "Dynamic modifier client tag to remove"
        };
        
        _useQuestAsOriginatorCheckBox = new CheckBox {
            IsChecked = _result.m_useQuestAsOriginator,
            Content = "Use quest as originator"
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        var clientTagPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Client Tag to Remove:" },
                _clientTagInput,
                new TextBlock { 
                    Text = "Identifier of the dynamic modifier to remove from the client", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var optionsPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                _useQuestAsOriginatorCheckBox,
                new TextBlock { 
                    Text = "When enabled, the quest will be used as the originator for removal", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Remove Dynamic Modifier", clientTagPanel));
        MainPanel.Children.Add(CreateGroupBox("Options", optionsPanel));
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
            _result.m_useQuestAsOriginator = _useQuestAsOriginatorCheckBox.IsChecked ?? false;
            
            ResultSource.SetResult(_result);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save remove dynamic modifier: {ex.Message}")
                .Send();
        }
    }
}