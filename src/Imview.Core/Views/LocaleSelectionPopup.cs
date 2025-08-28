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
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Imview.Core.Common.Constants;
using Imview.Core.Services;

namespace Imview.Core.Views;

/// <summary>
/// Popup window for selecting locale entries from specified categories
/// </summary>
public class LocaleSelectionPopup : Window
{
    private readonly string? _preferredCategory;
    private readonly string? _currentValue;
    private string? _selectedValue;
    
    private TextBox _filterBox = null!;
    private ListBox _entriesList = null!;
    private StackPanel _activeFiltersPanel = null!;
    private Dictionary<string, Dictionary<string, string>> _allCategoryData = new();
    private ObservableCollection<LocaleDisplayEntry> _filteredEntries = new();
    private HashSet<string> _activeCategoryFilters = new();

    public LocaleSelectionPopup(string? preferredCategory = null, string? currentValue = null)
    {
        _preferredCategory = preferredCategory;
        _currentValue = currentValue;
        
        InitializeWindow();
        LoadLocaleEntries();
        InitializeControls();
        ApplyFilter();
        HighlightCurrentValue();
    }

    private void InitializeWindow()
    {
        Title = "Select Locale Entry";
        Width = 800;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = true;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    }

    private void LoadLocaleEntries()
    {
        if (!LocaleService.Instance.IsLoaded)
        {
            return;
        }

        // Load all categories into our data dictionary
        var allCategories = LocaleService.Instance.GetCategoryNames();
        foreach (var category in allCategories)
        {
            var categoryEntries = LocaleService.Instance.GetCategory(category);
            _allCategoryData[category] = categoryEntries;
        }

        // Start with only the preferred category active (if it exists)
        if (!string.IsNullOrEmpty(_preferredCategory) && _allCategoryData.ContainsKey(_preferredCategory))
        {
            _activeCategoryFilters.Add(_preferredCategory);
        }
        else
        {
            // If no preferred category or it doesn't exist, show all categories
            foreach (var category in allCategories)
            {
                _activeCategoryFilters.Add(category);
            }
        }
    }

    private void InitializeControls()
    {
        var mainPanel = new DockPanel
        {
            Margin = EditorConstants.DEFAULT_MARGIN_THICKNESS
        };

        // Create filter section
        var filterPanel = CreateFilterPanel();
        DockPanel.SetDock(filterPanel, Dock.Top);

        // Create entries list
        var listPanel = CreateEntriesListPanel();

        // Create action buttons
        var buttonPanel = CreateButtonPanel();
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        mainPanel.Children.Add(filterPanel);
        mainPanel.Children.Add(buttonPanel);
        mainPanel.Children.Add(listPanel);

        Content = mainPanel;
    }

    private Control CreateFilterPanel()
    {
        var panel = new StackPanel
        {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Margin = new Thickness(0, 0, 0, 15)
        };

        var headerText = new TextBlock
        {
            Text = "Locale Entry Selection",
            FontWeight = FontWeight.Bold,
            FontSize = 16
        };

        // Active filters section
        var filtersHeaderPanel = new DockPanel();
        
        var filtersLabel = new TextBlock
        {
            Text = "Active Category Filters:",
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var addFilterButton = new Button
        {
            Content = "Add Category",
            Padding = new Thickness(8, 4),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        addFilterButton.Click += ShowAddCategoryDialog;

        filtersHeaderPanel.Children.Add(filtersLabel);
        filtersHeaderPanel.Children.Add(addFilterButton);
        DockPanel.SetDock(filtersLabel, Dock.Left);

        _activeFiltersPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Margin = new Thickness(0, 5, 0, 10)
        };

        // Text filter
        var filterLabel = new TextBlock
        {
            Text = "Text Filter:",
            Margin = new Thickness(0, 10, 0, 0)
        };

        _filterBox = new TextBox
        {
            Watermark = "Type to filter by key or value..."
        };
        
        _filterBox.TextChanged += (s, e) => ApplyFilter();

        panel.Children.Add(headerText);
        panel.Children.Add(filtersHeaderPanel);
        panel.Children.Add(_activeFiltersPanel);
        panel.Children.Add(filterLabel);
        panel.Children.Add(_filterBox);

        return panel;
    }

    private Control CreateEntriesListPanel()
    {
        _entriesList = new ListBox
        {
            ItemsSource = _filteredEntries,
            SelectionMode = SelectionMode.Single
        };

        // Create custom item template
        _entriesList.ItemTemplate = new FuncDataTemplate<LocaleDisplayEntry>((entry, _) =>
        {
            if (entry == null) return null;

            var panel = new DockPanel
            {
                Margin = new Thickness(5)
            };

            // Key/Reference on the left
            var keyText = new TextBlock
            {
                Text = entry.FullReference,
                FontFamily = new FontFamily("Consolas, monospace"),
                FontWeight = FontWeight.SemiBold,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Value on the right
            var valueText = new TextBlock
            {
                Text = entry.Value,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            panel.Children.Add(keyText);
            panel.Children.Add(valueText);
            DockPanel.SetDock(keyText, Dock.Left);

            return panel;
        });

        _entriesList.DoubleTapped += (s, e) => SelectAndClose();

        return new ScrollViewer
        {
            Content = _entriesList
        };
    }

    private Control CreateButtonPanel()
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 15, 0, 0),
            Children =
            {
                new Button
                {
                    Content = "Select",
                    Padding = new Thickness(20, 10),
                    Command = ReactiveCommand.Create(SelectAndClose)
                },
                new Button
                {
                    Content = "Cancel",
                    Padding = new Thickness(20, 10),
                    Command = ReactiveCommand.Create(() => Close())
                }
            }
        };
    }

    private void ApplyFilter()
    {
        var filterText = _filterBox?.Text?.ToLowerInvariant() ?? "";
        
        _filteredEntries.Clear();
        
        // Build entries from active category filters
        var allEntries = new List<LocaleDisplayEntry>();
        
        foreach (var category in _activeCategoryFilters)
        {
            if (_allCategoryData.TryGetValue(category, out var categoryData))
            {
                foreach (var entry in categoryData)
                {
                    allEntries.Add(new LocaleDisplayEntry
                    {
                        Category = category,
                        Key = entry.Key,
                        Value = entry.Value,
                        FullReference = $"{category}_{entry.Key}"
                    });
                }
            }
        }
        
        // Apply text filter
        var filtered = string.IsNullOrEmpty(filterText)
            ? allEntries
            : allEntries.Where(entry => 
                entry.FullReference.ToLowerInvariant().Contains(filterText) ||
                entry.Value.ToLowerInvariant().Contains(filterText));

        // Sort alphabetically by category then key
        var sorted = filtered.OrderBy(e => e.Category)
                            .ThenBy(e => e.Key);

        foreach (var entry in sorted)
        {
            _filteredEntries.Add(entry);
        }
        
        // Update the active filters display
        UpdateActiveFiltersDisplay();
    }

    private void UpdateActiveFiltersDisplay()
    {
        _activeFiltersPanel.Children.Clear();
        
        foreach (var category in _activeCategoryFilters.OrderBy(c => c))
        {
            var filterChip = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 80, 120)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 5, 0)
            };

            var chipPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            var categoryText = new TextBlock
            {
                Text = category,
                Foreground = Brushes.White,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            var removeButton = new Button
            {
                Content = "×",
                Width = 16,
                Height = 16,
                FontSize = 10,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            var capturedCategory = category;
            removeButton.Click += (s, e) => RemoveCategoryFilter(capturedCategory);

            chipPanel.Children.Add(categoryText);
            chipPanel.Children.Add(removeButton);
            filterChip.Child = chipPanel;

            _activeFiltersPanel.Children.Add(filterChip);
        }
    }

    private void RemoveCategoryFilter(string category)
    {
        _activeCategoryFilters.Remove(category);
        ApplyFilter();
    }

    private async void ShowAddCategoryDialog(object? sender, EventArgs e)
    {
        var availableCategories = _allCategoryData.Keys
            .Where(c => !_activeCategoryFilters.Contains(c))
            .OrderBy(c => c)
            .ToList();

        if (!availableCategories.Any())
        {
            // Show message that all categories are already active
            return;
        }

        // Create a simple category selection dialog
        var dialog = new Window
        {
            Title = "Add Category Filter",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var listBox = new ListBox
        {
            ItemsSource = availableCategories,
            Margin = new Thickness(10)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(10),
            Children =
            {
                new Button
                {
                    Content = "Add",
                    Command = ReactiveCommand.Create(() =>
                    {
                        if (listBox.SelectedItem is string selectedCategory)
                        {
                            _activeCategoryFilters.Add(selectedCategory);
                            ApplyFilter();
                            dialog.Close();
                        }
                    })
                },
                new Button
                {
                    Content = "Cancel",
                    Command = ReactiveCommand.Create(() => dialog.Close())
                }
            }
        };

        var mainPanel = new DockPanel();
        mainPanel.Children.Add(buttonPanel);
        mainPanel.Children.Add(listBox);
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        dialog.Content = mainPanel;
        await dialog.ShowDialog(this);
    }

    private void HighlightCurrentValue()
    {
        if (string.IsNullOrEmpty(_currentValue))
            return;

        var matchingEntry = _filteredEntries.FirstOrDefault(e => e.FullReference == _currentValue);
        if (matchingEntry != null)
        {
            _entriesList.SelectedItem = matchingEntry;
            _entriesList.ScrollIntoView(matchingEntry);
        }
    }

    private void SelectAndClose()
    {
        if (_entriesList.SelectedItem is LocaleDisplayEntry selected)
        {
            _selectedValue = selected.FullReference;
        }
        Close(_selectedValue);
    }

    /// <summary>
    /// Gets the selected locale reference (e.g., "QuestTitle_1625CA")
    /// </summary>
    public string? GetSelectedValue() => _selectedValue;
}

/// <summary>
/// Display model for locale entries in the selection popup
/// </summary>
public class LocaleDisplayEntry
{
    public string Category { get; set; } = "";
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string FullReference { get; set; } = "";
}