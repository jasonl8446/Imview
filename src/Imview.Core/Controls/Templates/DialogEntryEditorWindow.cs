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
   this software without specific products derived from
   this software without specific prior written permission.
*/

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ReactiveUI;
using System;
using System.Linq;
using Imview.Core.Common.Constants;
using Imview.Core.Services;

namespace Imview.Core.Controls.Templates;

/// <summary>
/// Window for editing individual dialog entries with all their properties
/// </summary>
public class DialogEntryEditorWindow : Window {
    
    private DialogEntryWrapper _result;
    private DialogEntryWrapper _originalEntry;
    
    // UI Controls
    private TextBox _personaNameBox = null!;
    private TextBox _dialogKeyBox = null!;
    private TextBox _soundFileBox = null!;
    private NumericUpDown _actorTemplateIdBox = null!;
    private TextBox _cameraNameBox = null!;
    private TextBox _pictureBox = null!;
    private TextBox _actionBox = null!;
    private TextBox _nameOverrideBox = null!;
    private TextBox _guiDisplayBox = null!;

    public DialogEntryEditorWindow(DialogEntryWrapper entry) {
        _originalEntry = entry;
        _result = new DialogEntryWrapper {
            PersonaName = entry.PersonaName,
            DialogKey = entry.DialogKey,
            SoundFile = entry.SoundFile,
            ActorTemplateID = entry.ActorTemplateID,
            CameraName = entry.CameraName,
            Picture = entry.Picture,
            Action = entry.Action,
            NameOverride = entry.NameOverride,
            GuiDisplay = entry.GuiDisplay
        };

        InitializeWindow();
        InitializeControls();
        PopulateControls();
    }

    private void InitializeWindow() {
        Title = "Edit Dialog Entry";
        Width = 500;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = true;
        
        // Set window icon and styling
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    }

    private void InitializeControls() {
        var scrollViewer = new ScrollViewer();
        var mainPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Margin = EditorConstants.DEFAULT_MARGIN_THICKNESS
        };

        // Create form sections
        mainPanel.Children.Add(CreateBasicInfoSection());
        mainPanel.Children.Add(CreateAdvancedSection());
        mainPanel.Children.Add(CreateActionButtons());

        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    private Control CreateBasicInfoSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Persona Name:", 
                    "The name of the NPC persona that will speak this dialogue", 
                    _personaNameBox = new TextBox()),
                    
                CreateDialogKeySection(),
                    
                CreateLabeledControl("Sound File Path:", 
                    "Path to the audio file for voice acting (e.g., '|Sound_Dialogue|WorldData|Sound/Dialogue/MerleAmbrose_QST_13.mp3')", 
                    _soundFileBox = new TextBox()),
                    
                CreateLabeledControl("Actor Template ID:", 
                    "Unique identifier for the NPC actor template (0 = no specific actor)", 
                    _actorTemplateIdBox = new NumericUpDown { 
                        Minimum = 0, 
                        Maximum = int.MaxValue,
                        Value = 0 
                    })
            }
        };

        return CreateGroupBox("Basic Dialog Properties", content);
    }

    private Control CreateDialogKeySection() {
        var panel = new StackPanel { Spacing = 3 };
        
        var headerText = new TextBlock { 
            Text = "Dialog Locale Key:",
            FontWeight = FontWeight.SemiBold
        };
        
        var explanationTextBlock = new TextBlock {
            Text = "The localization key that references the actual dialogue text (e.g., 'WizQst9559_00000129')",
            Foreground = Brushes.LightGray,
            FontStyle = FontStyle.Italic,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 5)
        };
        
        // Text box for editing the locale ID
        _dialogKeyBox = new TextBox();
        
        // Text block to display the resolved English text
        var resolvedTextBlock = new TextBlock {
            Foreground = Brushes.LightGray,
            FontStyle = FontStyle.Italic,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        // Update resolved text when the text box changes
        _dialogKeyBox.TextChanged += (sender, e) => {
            var localeReference = _dialogKeyBox.Text;
            if (string.IsNullOrEmpty(localeReference)) {
                resolvedTextBlock.Text = "";
                return;
            }
            
            var resolvedText = ResolveLocaleString(localeReference);
            if (!string.IsNullOrEmpty(resolvedText) && resolvedText != localeReference) {
                resolvedTextBlock.Text = $"➤ {resolvedText}";
            } else {
                resolvedTextBlock.Text = "➤ (No locale text found)";
            }
        };
        
        panel.Children.Add(headerText);
        panel.Children.Add(explanationTextBlock);
        panel.Children.Add(_dialogKeyBox);
        panel.Children.Add(resolvedTextBlock);
        
        return panel;
    }

    private string? ResolveLocaleString(string localeReference) {
        if (string.IsNullOrEmpty(localeReference) || !LocaleService.Instance.IsLoaded) {
            return null;
        }

        // Split the locale reference into category and key (e.g., "WizQst9559_00000129")
        var underscoreIndex = localeReference.LastIndexOf('_');
        if (underscoreIndex == -1) {
            return null; // Invalid format
        }

        var category = localeReference.Substring(0, underscoreIndex);
        var key = localeReference.Substring(underscoreIndex + 1);

        // Pad the key to 8 digits if it's not already and is all numeric
        if (key.Length < 8 && key.All(char.IsDigit)) {
            key = key.PadLeft(8, '0');
        }

        return LocaleService.Instance.GetString(category, key);
    }

    private Control CreateAdvancedSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Camera Name:", 
                    "Camera angle or position for this dialogue (optional)", 
                    _cameraNameBox = new TextBox()),
                    
                CreateLabeledControl("Picture/Icon:", 
                    "Image or icon to display with this dialogue (e.g., GUI/NpcPortraits/Art_Portrait_Ambrose.dds)", 
                    _pictureBox = new TextBox()),
                    
                CreateLabeledControl("Action:", 
                    "Special action or animation to perform during dialogue (optional)", 
                    _actionBox = new TextBox()),
                    
                CreateLabeledControl("Name Override:", 
                    "Override the displayed name of the speaker (optional)", 
                    _nameOverrideBox = new TextBox()),
                    
                CreateLabeledControl("GUI Display:", 
                    "Special GUI display settings for this dialogue (optional)", 
                    _guiDisplayBox = new TextBox())
            }
        };

        return CreateGroupBox("Advanced Properties", content);
    }

    private Control CreateActionButtons() {
        return new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0),
            Children = {
                new Button {
                    Content = "Save",
                    Padding = new Thickness(20, 10),
                    Command = ReactiveCommand.Create(SaveAndClose)
                },
                new Button {
                    Content = "Cancel",
                    Padding = new Thickness(20, 10),
                    Command = ReactiveCommand.Create(() => Close(null))
                }
            }
        };
    }

    private static Control CreateLabeledControl(string labelText, string explanationText, Control control) {
        var panel = new StackPanel { 
            Spacing = 3
        };
        
        var headerText = new TextBlock { 
            Text = labelText,
            FontWeight = FontWeight.SemiBold
        };
        
        var explanationTextBlock = new TextBlock {
            Text = explanationText,
            Foreground = Brushes.LightGray,
            FontStyle = FontStyle.Italic,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 5)
        };
        
        panel.Children.Add(headerText);
        panel.Children.Add(explanationTextBlock);
        panel.Children.Add(control);
        
        return panel;
    }

    private static Border CreateGroupBox(string header, Control content) =>
        new() {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = EditorConstants.DEFAULT_CORNER_RADIUS,
            Padding = EditorConstants.DEFAULT_GROUP_PADDING,
            Margin = new Thickness(0, 0, 0, 15),
            Child = new StackPanel {
                Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
                Children = {
                    new TextBlock {
                        Text = header,
                        FontWeight = FontWeight.Bold,
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 8)
                    },
                    content
                }
            }
        };

    private void PopulateControls() {
        _personaNameBox.Text = _result.PersonaName;
        _dialogKeyBox.Text = _result.DialogKey;
        _soundFileBox.Text = _result.SoundFile;
        _actorTemplateIdBox.Value = _result.ActorTemplateID;
        _cameraNameBox.Text = _result.CameraName;
        _pictureBox.Text = _result.Picture;
        _actionBox.Text = _result.Action;
        _nameOverrideBox.Text = _result.NameOverride;
        _guiDisplayBox.Text = _result.GuiDisplay;
    }

    private void SaveAndClose() {
        // Update the result with current values
        _result.PersonaName = _personaNameBox.Text ?? "";
        _result.DialogKey = _dialogKeyBox.Text ?? "";
        _result.SoundFile = _soundFileBox.Text ?? "";
        _result.ActorTemplateID = (int)(_actorTemplateIdBox.Value ?? 0);
        _result.CameraName = _cameraNameBox.Text ?? "";
        _result.Picture = _pictureBox.Text ?? "";
        _result.Action = _actionBox.Text ?? "";
        _result.NameOverride = _nameOverrideBox.Text ?? "";
        _result.GuiDisplay = _guiDisplayBox.Text ?? "";

        Close(_result);
    }
}