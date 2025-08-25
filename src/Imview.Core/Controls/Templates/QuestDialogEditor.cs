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
using Avalonia.Media;
using System.Collections.ObjectModel;
using Imcodec.ObjectProperty.TypeCache;
using System;
using System.Collections.Generic;
using Imview.Core.Common.Constants;
using System.Linq;
using ReactiveUI;
using Avalonia.Controls.Templates;

namespace Imview.Core.Controls.Templates;

/// <summary>
/// Enhanced dialog editor for Quest templates that handles the proper dialogue system
/// with ActorDialogList, ActorDialog, and NPCDialogEntry structures.
/// </summary>
public partial class QuestDialogEditor : UserControl {

    public static readonly StyledProperty<ActorDialogList> DialogListProperty =
        AvaloniaProperty.Register<QuestDialogEditor, ActorDialogList>(nameof(DialogList));

    public ActorDialogList DialogList {
        get => GetValue(DialogListProperty);
        set {
            SetValue(DialogListProperty, value);
            if (value != null) {
                PopulateDialogSections(value);
            }
        }
    }

    // Dialog section collections - one for each dialog tag
    private readonly ObservableCollection<DialogEntryWrapper> _prepDialogEntries;
    private readonly ObservableCollection<DialogEntryWrapper> _questInfoDialogEntries;
    private readonly ObservableCollection<DialogEntryWrapper> _underwayDialogEntries;
    private readonly ObservableCollection<DialogEntryWrapper> _completionDialogEntries;
    private readonly ObservableCollection<DialogEntryWrapper> _hyperlinkDialogEntries;

    // UI Controls for each section
    private ListBox _prepDialogList = null!;
    private ListBox _questInfoDialogList = null!; 
    private ListBox _underwayDialogList = null!;
    private ListBox _completionDialogList = null!;
    private ListBox _hyperlinkDialogList = null!;

    public QuestDialogEditor() {
        // Initialize collections
        _prepDialogEntries = new ObservableCollection<DialogEntryWrapper>();
        _questInfoDialogEntries = new ObservableCollection<DialogEntryWrapper>();
        _underwayDialogEntries = new ObservableCollection<DialogEntryWrapper>();
        _completionDialogEntries = new ObservableCollection<DialogEntryWrapper>();
        _hyperlinkDialogEntries = new ObservableCollection<DialogEntryWrapper>();

        InitializeComponent();
    }

    private void InitializeComponent() {
        var mainPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Margin = EditorConstants.DEFAULT_MARGIN_THICKNESS
        };

        // Create sections for each dialog tag
        mainPanel.Children.Add(CreateDialogSection(
            "Quest Info", 
            "Dialogue shown when the quest giver first explains the quest objectives and story context.",
            _questInfoDialogEntries, 
            out _questInfoDialogList));

        mainPanel.Children.Add(CreateDialogSection(
            "Quest Prep", 
            "Initial dialogue when the quest is first offered to the player, before acceptance.",
            _prepDialogEntries, 
            out _prepDialogList));

        mainPanel.Children.Add(CreateDialogSection(
            "Quest Underway", 
            "Dialogue shown when the player talks to the quest giver while the quest is still active.",
            _underwayDialogEntries, 
            out _underwayDialogList));

        mainPanel.Children.Add(CreateDialogSection(
            "Quest Completion", 
            "Dialogue shown when the player returns to complete the quest and claim rewards.",
            _completionDialogEntries, 
            out _completionDialogList));

        mainPanel.Children.Add(CreateDialogSection(
            "Hyperlink (Optional)", 
            "Additional optional dialogue that can be accessed through hyperlinks or special interactions.",
            _hyperlinkDialogEntries, 
            out _hyperlinkDialogList));

        Content = new ScrollViewer { Content = mainPanel };
    }

    private Control CreateDialogSection(string title, string explanationText, 
        ObservableCollection<DialogEntryWrapper> dialogEntries, out ListBox dialogList) {
        
        // Create the explanation info panel (like in Quest Goals section)
        var infoPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 5),
            Children = {
                new TextBlock {
                    Text = explanationText,
                    Foreground = Brushes.LightGray,
                    FontStyle = FontStyle.Italic
                }
            }
        };

        // Create the dialog list
        dialogList = new ListBox {
            ItemsSource = dialogEntries,
            Height = 150,
            ItemTemplate = new FuncDataTemplate<DialogEntryWrapper>((entry, _) => {
                if (entry == null) return null;

                var panel = new StackPanel { Spacing = 2 };
                
                // Show persona name and dialog key
                var headerText = new TextBlock {
                    Text = $"Persona: {entry.PersonaName ?? "(No Persona)"}",
                    FontWeight = FontWeight.SemiBold
                };
                
                var dialogText = new TextBlock {
                    Text = $"Dialog Key: {entry.DialogKey ?? "(No Dialog Key)"}",
                    Foreground = Brushes.Gray,
                    FontSize = 11
                };

                var soundText = new TextBlock {
                    Text = $"Sound: {entry.SoundFile ?? "(No Sound)"}",
                    Foreground = Brushes.LightBlue,
                    FontSize = 10
                };

                panel.Children.Add(headerText);
                panel.Children.Add(dialogText);
                panel.Children.Add(soundText);

                return panel;
            })
        };

        // Capture variables for lambda use
        var capturedDialogList = dialogList;
        var capturedDialogEntries = dialogEntries;
        
        // Add double-click handler to edit entries
        capturedDialogList.DoubleTapped += async (s, e) => {
            if (capturedDialogList.SelectedItem is DialogEntryWrapper selectedEntry) {
                var result = await ShowDialogEntryEditor(selectedEntry);
                if (result != null) {
                    var index = capturedDialogEntries.IndexOf(selectedEntry);
                    capturedDialogEntries[index] = result;
                }
            }
        };

        // Create buttons for managing dialog entries
        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Margin = new Thickness(0, 5, 0, 0),
            Children = {
                new Avalonia.Controls.Button {
                    Content = "Add Entry",
                    Command = ReactiveCommand.Create(() => AddDialogEntry(capturedDialogEntries))
                },
                new Avalonia.Controls.Button {
                    Content = "Remove Selected",
                    Command = ReactiveCommand.Create(() => RemoveSelectedDialogEntry(capturedDialogList, capturedDialogEntries))
                },
                new Avalonia.Controls.Button {
                    Content = "Move Up",
                    Command = ReactiveCommand.Create(() => MoveDialogEntry(capturedDialogList, capturedDialogEntries, -1))
                },
                new Avalonia.Controls.Button {
                    Content = "Move Down",
                    Command = ReactiveCommand.Create(() => MoveDialogEntry(capturedDialogList, capturedDialogEntries, 1))
                }
            }
        };

        var content = new DockPanel {
            LastChildFill = true,
            Children = {
                infoPanel,
                buttonPanel,
                dialogList
            }
        };
        
        DockPanel.SetDock(infoPanel, Dock.Top);
        DockPanel.SetDock(buttonPanel, Dock.Top);

        return CreateGroupBox(title, content);
    }

    private static Border CreateGroupBox(string header, Control content)
        => new() {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = EditorConstants.DEFAULT_CORNER_RADIUS,
            Padding = EditorConstants.DEFAULT_GROUP_PADDING,
            Margin = new Thickness(0, 0, 0, 10),
            Child = new StackPanel {
                Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
                Children = {
                    new TextBlock {
                        Text = header,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 0, 0, 5)
                    },
                    content
                }
            }
        };

    private async void AddDialogEntry(ObservableCollection<DialogEntryWrapper> entries) {
        var newEntry = new DialogEntryWrapper {
            PersonaName = "",
            DialogKey = "",
            SoundFile = "",
            ActorTemplateID = 0
        };

        var result = await ShowDialogEntryEditor(newEntry);
        if (result != null) {
            entries.Add(result);
        }
    }

    private void RemoveSelectedDialogEntry(ListBox listBox, ObservableCollection<DialogEntryWrapper> entries) {
        if (listBox.SelectedItem is DialogEntryWrapper selectedEntry) {
            entries.Remove(selectedEntry);
        }
    }

    private void MoveDialogEntry(ListBox listBox, ObservableCollection<DialogEntryWrapper> entries, int direction) {
        if (listBox.SelectedItem is DialogEntryWrapper selectedEntry) {
            var currentIndex = entries.IndexOf(selectedEntry);
            var newIndex = currentIndex + direction;

            if (newIndex >= 0 && newIndex < entries.Count) {
                entries.RemoveAt(currentIndex);
                entries.Insert(newIndex, selectedEntry);
                listBox.SelectedIndex = newIndex;
            }
        }
    }

    private async System.Threading.Tasks.Task<DialogEntryWrapper?> ShowDialogEntryEditor(DialogEntryWrapper entry) {
        // Create a simple dialog entry editor window
        var editor = new DialogEntryEditorWindow(entry);
        
        // Show as dialog
        var appLifetime = Application.Current?.ApplicationLifetime as 
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        
        if (appLifetime?.MainWindow != null) {
            var result = await editor.ShowDialog<DialogEntryWrapper?>(appLifetime.MainWindow);
            return result;
        }

        return null;
    }

    private void PopulateDialogSections(ActorDialogList dialogList) {
        // Clear existing entries
        _prepDialogEntries.Clear();
        _questInfoDialogEntries.Clear();
        _underwayDialogEntries.Clear();
        _completionDialogEntries.Clear();
        _hyperlinkDialogEntries.Clear();

        if (dialogList?.m_dialogs == null) return;

        // Populate each section based on dialog tags
        foreach (var dialog in dialogList.m_dialogs) {
            if (dialog?.m_dialogEntries == null) continue;

            var targetCollection = dialog.m_dialogTag?.ToLower() switch {
                "prep" => _prepDialogEntries,
                "questinfo" => _questInfoDialogEntries,
                "underway" => _underwayDialogEntries,
                "completion" => _completionDialogEntries,
                "hyperlink" => _hyperlinkDialogEntries,
                _ => _questInfoDialogEntries // Default to quest info
            };

            foreach (var entry in dialog.m_dialogEntries) {
                if (entry is NPCDialogEntry npcEntry) {
                    targetCollection.Add(new DialogEntryWrapper {
                        PersonaName = npcEntry.m_personaName ?? "",
                        DialogKey = npcEntry.m_dialog ?? "",
                        SoundFile = npcEntry.m_soundFile ?? "",
                        ActorTemplateID = (int)npcEntry.m_actorTemplateID,
                        CameraName = npcEntry.m_cameraName ?? "",
                        Picture = npcEntry.m_picture ?? "",
                        Action = npcEntry.m_action ?? "",
                        NameOverride = npcEntry.m_nameOverride ?? "",
                        GuiDisplay = npcEntry.m_guiDisplay ?? ""
                    });
                }
            }
        }
    }

    /// <summary>
    /// Converts the current dialog entries back to ActorDialogList format
    /// </summary>
    public ActorDialogList ToActorDialogList() {
        var dialogList = new ActorDialogList {
            m_dialogs = new List<ActorDialog>()
        };

        // Create dialog sections for each type
        var sections = new[] {
            ("Prep", _prepDialogEntries),
            ("QuestInfo", _questInfoDialogEntries), 
            ("Underway", _underwayDialogEntries),
            ("Completion", _completionDialogEntries),
            ("Hyperlink", _hyperlinkDialogEntries)
        };

        foreach (var (tag, entries) in sections) {
            if (entries.Count > 0) {
                var dialog = new ActorDialog {
                    m_dialogTag = tag,
                    m_dialogEntries = entries.Select(wrapper => new NPCDialogEntry {
                        m_personaName = wrapper.PersonaName,
                        m_dialog = wrapper.DialogKey,
                        m_soundFile = wrapper.SoundFile,
                        m_actorTemplateID = (uint)wrapper.ActorTemplateID,
                        m_cameraName = wrapper.CameraName,
                        m_picture = wrapper.Picture,
                        m_action = wrapper.Action,
                        m_nameOverride = wrapper.NameOverride,
                        m_guiDisplay = wrapper.GuiDisplay,
                        m_interpolationDuration = 0.0f
                    }).Cast<ActorDialogEntry>().ToList()
                };
                dialogList.m_dialogs.Add(dialog);
            }
        }

        return dialogList;
    }
}

/// <summary>
/// Wrapper class for dialog entries to make UI binding easier
/// </summary>
public class DialogEntryWrapper {
    public string PersonaName { get; set; } = "";
    public string DialogKey { get; set; } = "";
    public string SoundFile { get; set; } = "";
    public int ActorTemplateID { get; set; }
    public string CameraName { get; set; } = "";
    public string Picture { get; set; } = "";
    public string Action { get; set; } = "";
    public string NameOverride { get; set; } = "";
    public string GuiDisplay { get; set; } = "";
}