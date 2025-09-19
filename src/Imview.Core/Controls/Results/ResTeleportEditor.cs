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

public class ResTeleportEditor : EditorWindowBase<ResTeleport> {
    
    private readonly ResTeleport _result;
    private readonly TextBox _destinationLocInput;
    private readonly TextBox _destinationZoneInput;
    private readonly NumericUpDown _exitTeleporterInput;
    private readonly NumericUpDown _teleporterTagInput;
    private readonly ComboBox _teleportTypeComboBox;
    private readonly NumericUpDown _transitionIdInput;

    public ResTeleportEditor(ResTeleport? result = null) 
        : base(result is not null ? "Edit Teleport" : "Add Teleport") {
        
        _result = result ?? new ResTeleport();
        
        Width = EditorConstants.DEFAULT_WINDOW_WIDTH;
        Height = 500;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _destinationLocInput = new TextBox {
            Text = _result.m_destinationLoc ?? "",
            Watermark = "Destination location name"
        };
        
        _destinationZoneInput = new TextBox {
            Text = _result.m_destinationZone ?? "",
            Watermark = "Destination zone name"
        };
        
        _exitTeleporterInput = new NumericUpDown {
            Value = _result.m_exitTeleporter,
            Minimum = 0,
            Maximum = 255,
            Increment = 1
        };
        
        _teleporterTagInput = new NumericUpDown {
            Value = _result.m_teleporterTag,
            Minimum = 0,
            Maximum = 255,
            Increment = 1
        };
        
        _teleportTypeComboBox = new ComboBox {
            ItemsSource = new[] { "TELEPORT_STATIC" },
            SelectedIndex = 0
        };
        
        _transitionIdInput = new NumericUpDown {
            Value = _result.m_transitionID,
            Minimum = 0,
            Maximum = 255,
            Increment = 1
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        var destinationPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Destination Location:" },
                _destinationLocInput,
                new TextBlock { 
                    Text = "The name of the destination location where the player will be teleported", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var zonePanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Destination Zone:" },
                _destinationZoneInput,
                new TextBlock { 
                    Text = "The name of the destination zone where the player will be teleported", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var teleporterPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Exit Teleporter:" },
                _exitTeleporterInput,
                new TextBlock { 
                    Text = "Exit teleporter identifier (0-255)", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var tagPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Teleporter Tag:" },
                _teleporterTagInput,
                new TextBlock { 
                    Text = "Teleporter tag identifier (0-255)", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var typePanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Teleport Type:" },
                _teleportTypeComboBox,
                new TextBlock { 
                    Text = "Type of teleport operation to perform", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        var transitionPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Transition ID:" },
                _transitionIdInput,
                new TextBlock { 
                    Text = "Transition identifier for the teleport effect (0-255)", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Destination Settings", destinationPanel));
        MainPanel.Children.Add(CreateGroupBox("Zone Settings", zonePanel));
        MainPanel.Children.Add(CreateGroupBox("Teleporter Configuration", teleporterPanel));
        MainPanel.Children.Add(CreateGroupBox("Teleporter Tag", tagPanel));
        MainPanel.Children.Add(CreateGroupBox("Teleport Type", typePanel));
        MainPanel.Children.Add(CreateGroupBox("Transition Settings", transitionPanel));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }
    
    private void Save() {
        try {
            _result.m_destinationLoc = _destinationLocInput.Text ?? string.Empty;
            _result.m_destinationZone = _destinationZoneInput.Text ?? string.Empty;
            _result.m_exitTeleporter = (byte)(_exitTeleporterInput.Value ?? 0);
            _result.m_teleporterTag = (byte)(_teleporterTagInput.Value ?? 0);
            // _result.m_teleportType = TeleportType.TELEPORT_STATIC; // Will use default value
            _result.m_transitionID = (byte)(_transitionIdInput.Value ?? 0);
            
            ResultSource.SetResult(_result);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save teleport result: {ex.Message}")
                .Send();
        }
    }
}