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
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Imview.Core.Common.Constants;
using Imview.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Imview.Core.Controls.Templates;

/// <summary>
/// Window for browsing and selecting dialog entries from an imported packet capture.
/// </summary>
public class DialogPacketImportWindow : Window {

    private readonly ListBox _dialogListBox;
    private readonly TextBox _searchBox;
    private readonly ulong? _highlightMobileId;
    private readonly ulong? _highlightQuestId;
    private readonly ulong? _highlightGoalId;

    private ObservableCollection<CapturedDialogEntry> _allDialogs = [];
    private ObservableCollection<CapturedDialogEntry> _filteredDialogs = [];

    /// <summary>
    /// The dialog entry selected for import.
    /// </summary>
    public CapturedDialogEntry? SelectedEntry { get; private set; }

    /// <summary>
    /// Creates a new DialogPacketImportWindow.
    /// </summary>
    /// <param name="highlightMobileId">MobileID to highlight as matching current quest</param>
    /// <param name="highlightQuestId">QuestID to highlight as matching current quest</param>
    /// <param name="highlightGoalId">GoalID to highlight as matching current goal</param>
    public DialogPacketImportWindow(
        ulong? highlightMobileId = null,
        ulong? highlightQuestId = null,
        ulong? highlightGoalId = null) {

        Console.WriteLine("[DialogPacketImportWindow] Constructor called");

        _highlightMobileId = highlightMobileId;
        _highlightQuestId = highlightQuestId;
        _highlightGoalId = highlightGoalId;

        Title = "Import Dialog from Packet Capture";

        Width = 900;
        Height = 600;
        Background = new SolidColorBrush(Color.Parse("#1E1E1E"));

        _searchBox = new TextBox {
            Watermark = "Search dialogs by text, persona, type, or ID...",
            Margin = new Thickness(0, 0, 0, 10)
        };
        _searchBox.TextChanged += SearchBox_TextChanged;

        _dialogListBox = new ListBox {
            Height = 420,
            Background = Brushes.Transparent,
            ItemTemplate = CreateItemTemplate()
        };

        // Double-click to view in read-only mode
        _dialogListBox.DoubleTapped += DialogListBox_DoubleTapped;

        InitializeComponent();
        LoadDialogs();
    }

    private void InitializeComponent() {
        var mainPanel = new StackPanel {
            Spacing = 10,
            Margin = new Thickness(20)
        };

        // Header
        var headerText = new TextBlock {
            Text = "Select a Dialog Entry to Import",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };

        // Info text
        var infoText = new TextBlock {
            Text = "Double-click to view details. Select an entry and click Import to add it to your quest.",
            Foreground = Brushes.LightGray,
            FontStyle = FontStyle.Italic,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Locale warning if not loaded
        if (!LocaleService.Instance.IsLoaded) {
            var localeWarning = new Border {
                Background = new SolidColorBrush(Color.Parse("#3D3D00")),
                BorderBrush = Brushes.Gold,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock {
                    Text = "⚠ Locale data not loaded. Dialog text shows as locale keys. Load Root.wad for full text resolution.",
                    Foreground = Brushes.Gold,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            mainPanel.Children.Add(localeWarning);
        }

        // Search box
        var searchPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 5)
        };
        searchPanel.Children.Add(new TextBlock {
            Text = "Search:",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.SemiBold
        });
        searchPanel.Children.Add(_searchBox);

        // Highlight info
        if (_highlightMobileId.HasValue || _highlightQuestId.HasValue || _highlightGoalId.HasValue) {
            var highlightInfo = new TextBlock {
                Text = "💡 Entries matching your current quest are highlighted in green.",
                Foreground = Brushes.LightGreen,
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };
            mainPanel.Children.Add(highlightInfo);
        }

        mainPanel.Children.Add(headerText);
        mainPanel.Children.Add(infoText);
        mainPanel.Children.Add(searchPanel);
        mainPanel.Children.Add(_dialogListBox);

        // Buttons
        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var importButton = new Button {
            Content = "Import Selected",
            IsEnabled = false,
            Classes = { "accent" }
        };
        importButton.Click += (s, e) => ImportSelected();

        var viewButton = new Button {
            Content = "View Details",
            IsEnabled = false
        };
        viewButton.Click += (s, e) => ViewSelected();

        var cancelButton = new Button {
            Content = "Cancel"
        };
        cancelButton.Click += (s, e) => Close();

        buttonPanel.Children.Add(viewButton);
        buttonPanel.Children.Add(importButton);
        buttonPanel.Children.Add(cancelButton);

        mainPanel.Children.Add(buttonPanel);

        // Selection changed handler
        _dialogListBox.SelectionChanged += (s, e) => {
            var hasSelection = _dialogListBox.SelectedItem != null;
            importButton.IsEnabled = hasSelection;
            viewButton.IsEnabled = hasSelection;
        };

        Content = mainPanel;
    }

    private void LoadDialogs() {
        var service = DialogPacketImportService.Instance;

        Console.WriteLine($"[DialogPacketImportWindow] LoadDialogs called");
        Console.WriteLine($"[DialogPacketImportWindow] HasImportedPacketCapture: {service.HasImportedPacketCapture}");
        Console.WriteLine($"[DialogPacketImportWindow] CapturedDialogs.Count: {service.CapturedDialogs.Count}");

        if (!service.HasImportedPacketCapture) {
            var emptyText = new TextBlock {
                Text = "No packet capture has been imported.",
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _dialogListBox.Items.Add(emptyText);
            return;
        }

        // Store all dialogs
        _allDialogs = new ObservableCollection<CapturedDialogEntry>(service.CapturedDialogs);
        Console.WriteLine($"[DialogPacketImportWindow] _allDialogs.Count: {_allDialogs.Count}");

        // Sort: highlight matches first, then by completion type
        var sortedDialogs = _allDialogs.ToList();
        sortedDialogs.Sort((a, b) => {
            var aMatches = service.MatchesQuest(a, _highlightMobileId, _highlightQuestId) ||
                          service.MatchesGoal(a, _highlightGoalId);
            var bMatches = service.MatchesQuest(b, _highlightMobileId, _highlightQuestId) ||
                          service.MatchesGoal(b, _highlightGoalId);

            if (aMatches && !bMatches) return -1;
            if (!aMatches && bMatches) return 1;

            return string.Compare(a.CompletionType, b.CompletionType, StringComparison.OrdinalIgnoreCase);
        });

        Console.WriteLine($"[DialogPacketImportWindow] sortedDialogs.Count: {sortedDialogs.Count}");

        _filteredDialogs = new ObservableCollection<CapturedDialogEntry>(sortedDialogs);
        RefreshListBox();

        if (sortedDialogs.Count == 0) {
            var emptyText = new TextBlock {
                Text = "No dialogs found in the packet capture.",
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _dialogListBox.Items.Add(emptyText);
        }
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e) {
        FilterDialogs();
    }

    private void FilterDialogs() {
        var searchText = _searchBox.Text?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText)) {
            _filteredDialogs = new ObservableCollection<CapturedDialogEntry>(_allDialogs);
        } else {
            var filtered = _allDialogs.Where(d =>
                d.SearchText.ToLower().Contains(searchText) ||
                d.CompletionType.ToLower().Contains(searchText) ||
                d.QuestID.ToString().Contains(searchText) ||
                d.GoalID.ToString().Contains(searchText) ||
                d.MobileID.ToString().Contains(searchText)
            ).ToList();
            _filteredDialogs = new ObservableCollection<CapturedDialogEntry>(filtered);
        }

        RefreshListBox();
    }

    private void RefreshListBox() {
        Console.WriteLine($"[DialogPacketImportWindow] RefreshListBox called, _filteredDialogs.Count: {_filteredDialogs.Count}");
        _dialogListBox.Items.Clear();
        foreach (var dialog in _filteredDialogs) {
            _dialogListBox.Items.Add(dialog);
        }
        Console.WriteLine($"[DialogPacketImportWindow] ListBox.Items.Count: {_dialogListBox.Items.Count}");
    }

    private FuncDataTemplate<CapturedDialogEntry> CreateItemTemplate() {
        return new FuncDataTemplate<CapturedDialogEntry>((entry, _) => {
            if (entry == null) return null;

            var service = DialogPacketImportService.Instance;
            var isMatch = service.MatchesQuest(entry, _highlightMobileId, _highlightQuestId) ||
                         service.MatchesGoal(entry, _highlightGoalId);

            var panel = new StackPanel { Spacing = 3 };

            // NPC Name (resolved from locale) with entry index for multi-entry dialogs
            var npcName = !string.IsNullOrEmpty(entry.ResolvedPersonaName) && entry.ResolvedPersonaName != entry.PersonaName
                ? $"{entry.ResolvedPersonaName} ({entry.PersonaName})"
                : entry.PersonaName;

            // Add entry index indicator for multi-entry dialogs
            var npcNameWithIndex = entry.IsMultiEntry
                ? $"{npcName} [{entry.EntryIndex + 1}/{entry.TotalEntries}]"
                : npcName;

            var personaText = new TextBlock {
                Text = $"NPC: {npcNameWithIndex}",
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            // Dialog type
            var typeText = new TextBlock {
                Text = $"Type: {entry.CompletionType}",
                Foreground = GetCompletionTypeBrush(entry.CompletionType),
                FontSize = 11,
                FontWeight = FontWeight.Medium
            };

            // Dialog text (resolved English)
            var dialogText = new TextBlock {
                Text = !string.IsNullOrEmpty(entry.ResolvedDialogText) && entry.ResolvedDialogText != entry.DialogKey
                    ? $"\"{TruncateText(entry.ResolvedDialogText, 100)}\""
                    : !string.IsNullOrEmpty(entry.DialogKey)
                        ? $"[{entry.DialogKey}]"
                        : "(No dialog text)",
                Foreground = Brushes.LightGray,
                FontStyle = FontStyle.Italic,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 45
            };

            // IDs row
            var idText = new TextBlock {
                Text = $"QuestID: {entry.QuestID} | GoalID: {entry.GoalID} | MobileID: {entry.MobileID}",
                Foreground = Brushes.DimGray,
                FontSize = 10
            };

            panel.Children.Add(personaText);
            panel.Children.Add(typeText);
            panel.Children.Add(dialogText);
            panel.Children.Add(idText);

            // Highlight matching entries
            if (isMatch) {
                var border = new Border {
                    BorderBrush = Brushes.LightGreen,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 2),
                    Child = panel,
                    Background = new SolidColorBrush(Color.Parse("#1A2F1A"))
                };
                return border;
            }

            return new Border {
                Padding = new Thickness(8),
                Margin = new Thickness(0, 2),
                Child = panel
            };
        });
    }

    private static IBrush GetCompletionTypeBrush(string completionType) {
        return completionType?.ToLower() switch {
            "questinfo" => Brushes.Gold,
            "prep" => Brushes.LightBlue,
            "underway" => Brushes.Orange,
            "completion" => Brushes.LightGreen,
            "hyperlink" => Brushes.Plum,
            _ => Brushes.Gray
        };
    }

    private static string TruncateText(string text, int maxLength) {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) {
            return text ?? "";
        }
        return text.Substring(0, maxLength) + "...";
    }

    private void DialogListBox_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ViewSelected();
    }

    private void ViewSelected() {
        if (_dialogListBox.SelectedItem is not CapturedDialogEntry entry) {
            return;
        }

        if (entry.DialogEntry == null) {
            MessageService.Warn("This entry does not have dialog data to display.")
                .Send();
            return;
        }

        // Open read-only DialogEntryEditorWindow
        var editor = new DialogEntryEditorWindow(entry.DialogEntry, null, true);
        _ = editor.ShowDialog(this);
    }

    private void ImportSelected() {
        Console.WriteLine($"[DialogPacketImportWindow] ImportSelected called");
        Console.WriteLine($"[DialogPacketImportWindow] SelectedItem type: {_dialogListBox.SelectedItem?.GetType().Name ?? "null"}");

        if (_dialogListBox.SelectedItem is CapturedDialogEntry entry) {
            Console.WriteLine($"[DialogPacketImportWindow] Entry selected: {entry.DialogKey}, HasDialogEntry: {entry.DialogEntry != null}");
            SelectedEntry = entry;
            Close(entry);
        } else {
            Console.WriteLine("[DialogPacketImportWindow] No valid entry selected");
        }
    }

}