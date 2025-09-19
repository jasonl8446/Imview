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
using Imview.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Imview.Core.Controls.Results;

public class ResultTypeSelectionDialog : EditorWindowBase<Result> {
    
    private readonly Dictionary<string, string> _resultTypes = new() {
        { "Add Gold", "ResAddGold" },
        { "Add Magic XP", "ResAddMagicXP" },
        { "Drop Table", "ResDropTable" },
        { "Add Dynamic Modifier", "ResAddDynaMod" },
        { "Remove Dynamic Modifier", "ResRemoveDynaMod" },
        { "Actor Dialog", "ResActorDialog" }
    };
    
    private readonly ListBox _typesList;

    public ResultTypeSelectionDialog() : base("Select Result Type") {
        
        Width = EditorConstants.SMALL_WINDOW_WIDTH;
        Height = 400;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _typesList = new ListBox {
            ItemsSource = _resultTypes.Keys,
            Height = 250
        };
        
        _typesList.DoubleTapped += (s, e) => {
            if (_typesList.SelectedItem != null) {
                _ = OpenSpecificEditor();
            }
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        var instructionPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { 
                    Text = "Select the type of result to create:",
                    FontWeight = Avalonia.Media.FontWeight.Bold
                },
                new TextBlock { 
                    Text = "Double-click on a result type to open its editor, or select and click Create.",
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var createButton = new Avalonia.Controls.Button {
            Content = "Create",
            Command = ReactiveCommand.CreateFromTask(OpenSpecificEditor),
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 100
        };
        
        var cancelButton = new Avalonia.Controls.Button {
            Content = "Cancel",
            Command = ReactiveCommand.Create(Cancel),
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 100
        };
        
        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { createButton, cancelButton }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Result Types", instructionPanel));
        MainPanel.Children.Add(CreateGroupBox("Available Types", _typesList));
        MainPanel.Children.Add(buttonPanel);
    }
    
    private async Task OpenSpecificEditor() {
        if (_typesList.SelectedItem is not string selectedTypeName) {
            MessageService
                .Info("Please select a result type.")
                .Send();
            return;
        }
        
        if (!_resultTypes.TryGetValue(selectedTypeName, out var resultTypeName)) {
            MessageService
                .Error("Unknown result type selected.")
                .Send();
            return;
        }
        
        try {
            Avalonia.Controls.Window? editor = resultTypeName switch {
                "ResAddGold" => new ResAddGoldEditor(),
                "ResAddMagicXP" => new ResAddMagicXPEditor(),
                "ResDropTable" => new ResDropTableEditor(),
                "ResAddDynaMod" => new ResAddDynaModEditor(),
                "ResRemoveDynaMod" => new ResRemoveDynaModEditor(),
                "ResActorDialog" => new ResActorDialogEditor(),
                _ => null
            };
            
            if (editor == null) {
                MessageService
                    .Error("Editor not available for this result type.")
                    .Send();
                return;
            }
            
            await editor.ShowDialog(this);
            var result = await ResultEditorFactory.GetResultFromEditor(editor);
            
            if (result != null) {
                ResultSource.SetResult(result);
                Close();
            }
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to open editor: {ex.Message}")
                .Send();
        }
    }
}