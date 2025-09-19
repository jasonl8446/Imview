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

public class ResAddGoldEditor : EditorWindowBase<ResAddGold> {
    
    private readonly ResAddGold _result;
    private readonly NumericUpDown _goldInput;
    private readonly TextBox _sourceTypeInput;

    public ResAddGoldEditor(ResAddGold? result = null) 
        : base(result is not null ? "Edit Gold Reward" : "Add Gold Reward") {
        
        _result = result ?? new ResAddGold();
        
        Width = EditorConstants.SMALL_WINDOW_WIDTH;
        Height = 300;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _goldInput = new NumericUpDown {
            Minimum = 0,
            Maximum = int.MaxValue,
            Value = _result.m_gold,
            FormatString = "N0"
        };
        
        _sourceTypeInput = new TextBox {
            Text = _result.m_sourceType.ToString(),
            Watermark = "Source type (optional)"
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        var goldPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Gold Amount:" },
                _goldInput
            }
        };
        
        var sourceTypePanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Source Type:" },
                _sourceTypeInput
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Gold Reward Settings", goldPanel));
        MainPanel.Children.Add(CreateGroupBox("Source Type", sourceTypePanel));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }
    
    private void Save() {
        try {
            _result.m_gold = (int)(_goldInput.Value ?? 0);
            _result.m_sourceType = _sourceTypeInput.Text ?? string.Empty;
            
            ResultSource.SetResult(_result);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save gold reward: {ex.Message}")
                .Send();
        }
    }
}