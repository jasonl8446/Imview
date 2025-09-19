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

namespace Imview.Core.Controls.Results;

public class ResPostEventEditor : EditorWindowBase<ResPostEvent> {
    
    private readonly ResPostEvent _result;
    private readonly TextBox _eventNameInput;

    public ResPostEventEditor(ResPostEvent? result = null) 
        : base(result is not null ? "Edit Post Event" : "Add Post Event") {
        
        _result = result ?? new ResPostEvent();
        
        Width = EditorConstants.DEFAULT_WINDOW_WIDTH;
        Height = 300;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _eventNameInput = new TextBox {
            Text = _result.m_eventName ?? "",
            Watermark = "Event name to post"
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        var eventPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Event Name:" },
                _eventNameInput,
                new TextBlock { 
                    Text = "The name of the event to post to the game system", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var instructionsPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { 
                    Text = "Post Event Instructions:",
                    FontWeight = Avalonia.Media.FontWeight.Bold
                },
                new TextBlock { 
                    Text = "• Events are used to trigger other game systems", 
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.LightGray
                },
                new TextBlock { 
                    Text = "• Event names should match events defined in the game configuration", 
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.LightGray
                },
                new TextBlock { 
                    Text = "• Events can trigger scripts, animations, or other quest logic", 
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.LightGray
                },
                new TextBlock { 
                    Text = "• Use descriptive names that clearly indicate the event's purpose", 
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.LightGray
                }
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Event Settings", eventPanel));
        MainPanel.Children.Add(CreateGroupBox("Usage Guidelines", instructionsPanel));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }
    
    private void Save() {
        try {
            if (string.IsNullOrWhiteSpace(_eventNameInput.Text)) {
                MessageService
                    .Info("Event name is required.")
                    .Send();
                return;
            }
            
            _result.m_eventName = _eventNameInput.Text.Trim();
            
            ResultSource.SetResult(_result);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save post event result: {ex.Message}")
                .Send();
        }
    }
}