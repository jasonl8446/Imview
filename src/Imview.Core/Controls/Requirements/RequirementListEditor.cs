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
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.Common.Constants;
using Imview.Core.Controls.Base;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;

namespace Imview.Core.Controls.Requirements;

/// <summary>
/// Editor for managing collections of requirements within a RequirementList.
/// </summary>
public class RequirementListEditor : EditorWindowBase<RequirementList> {

    // Core properties
    private readonly RequirementList _requirementList;
    private readonly ObservableCollection<RequirementWrapper> _requirements;
    private readonly IRequirementEditorFactory _requirementEditorFactory;

    // UI Controls
    private readonly ListBox _requirementsList;

    public RequirementListEditor(RequirementList? requirementList = null, IRequirementEditorFactory? requirementEditorFactory = null) 
        : base(requirementList is not null ? "Edit Requirement List" : "Create Requirement List") {
        
        _requirementList = requirementList ?? new RequirementList();
        _requirementEditorFactory = requirementEditorFactory ?? new RequirementEditorFactory();
        
        // Create requirement wrappers
        _requirements = new ObservableCollection<RequirementWrapper>(
            (_requirementList.m_requirements ?? []).Select(r => new RequirementWrapper(r))
        );

        Width = EditorConstants.DEFAULT_WINDOW_WIDTH;
        Height = EditorConstants.DEFAULT_WINDOW_HEIGHT;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;

        _requirementsList = new ListBox {
            ItemsSource = _requirements,
            Height = 300
        };

        InitializeComponent();
    }

    private void InitializeComponent() {
        // Create a DataTemplate for the requirement items
        _requirementsList.ItemTemplate = new FuncDataTemplate<RequirementWrapper>((requirement, _) => {
            if (requirement == null) {
                return null;
            }

            var panel = new DockPanel();

            // Requirement type and description
            var requirementText = new TextBlock {
                Text = GetRequirementDisplayText(requirement.Requirement),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 10, 0)
            };

            // Remove button
            var removeButton = new Avalonia.Controls.Button {
                Content = "Remove",
                Background = Brushes.LightCoral,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Command = ReactiveCommand.Create(() => RemoveRequirement(requirement))
            };

            panel.Children.Add(requirementText);
            panel.Children.Add(removeButton);
            DockPanel.SetDock(removeButton, Dock.Right);

            return panel;
        });

        _requirementsList.DoubleTapped += RequirementsList_DoubleTapped;

        var infoPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 5),
            Children = {
                new TextBlock {
                    Text = "Double-click a requirement to edit it, or use the buttons below to manage requirements.",
                    Foreground = Brushes.LightGray,
                    FontStyle = FontStyle.Italic,
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Margin = new Thickness(0, 0, 0, 10),
            Children = {
                new Avalonia.Controls.Button {
                    Content = "Add Requirement",
                    Command = ReactiveCommand.Create(AddRequirement)
                },
                new Avalonia.Controls.Button {
                    Content = "Remove Selected",
                    Command = ReactiveCommand.Create(RemoveSelectedRequirement)
                },
                new Avalonia.Controls.Button {
                    Content = "Clear All",
                    Command = ReactiveCommand.Create(ClearAllRequirements)
                }
            }
        };

        var content = new DockPanel {
            LastChildFill = true,
            Children = {
                infoPanel,
                buttonPanel,
                _requirementsList
            }
        };
        DockPanel.SetDock(infoPanel, Dock.Top);
        DockPanel.SetDock(buttonPanel, Dock.Top);

        MainPanel.Children.Add(CreateGroupBox("Requirements", content));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }

    private static string GetRequirementDisplayText(Requirement requirement) {
        var typeName = requirement.GetType().Name;
        
        // Try to get a meaningful description based on common properties
        var description = typeName;
        
        // Try to get specific property values that might be useful for display
        var type = requirement.GetType();
        
        // Look for common descriptive properties
        var nameProperty = type.GetProperty("m_name") ?? type.GetProperty("Name");
        if (nameProperty != null) {
            var name = nameProperty.GetValue(requirement)?.ToString();
            if (!string.IsNullOrEmpty(name)) {
                description += $" ({name})";
            }
        }
        
        var valueProperty = type.GetProperty("m_numericValue") ?? type.GetProperty("m_value");
        if (valueProperty != null) {
            var value = valueProperty.GetValue(requirement)?.ToString();
            if (!string.IsNullOrEmpty(value)) {
                description += $" = {value}";
            }
        }

        return description;
    }

    private async void AddRequirement() {
        var newRequirement = await _requirementEditorFactory.CreateEditor();
        if (newRequirement != null) {
            _requirements.Add(new RequirementWrapper(newRequirement));
        }
    }

    private async void RequirementsList_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (_requirementsList.SelectedItem is RequirementWrapper selectedWrapper) {
            var editedRequirement = await _requirementEditorFactory.CreateEditor(selectedWrapper.Requirement);
            if (editedRequirement != null) {
                var index = _requirements.IndexOf(selectedWrapper);
                _requirements[index] = new RequirementWrapper(editedRequirement);
            }
        }
    }

    private void RemoveRequirement(RequirementWrapper requirement) {
        _requirements.Remove(requirement);
    }

    private void RemoveSelectedRequirement() {
        if (_requirementsList.SelectedItem is RequirementWrapper selectedWrapper) {
            _requirements.Remove(selectedWrapper);
        }
    }

    private void ClearAllRequirements() {
        _requirements.Clear();
    }

    private void Save() {
        // Update the requirement list with current requirements
        _requirementList.m_requirements = _requirements.Select(wrapper => wrapper.Requirement).ToList();
        
        ResultSource.SetResult(_requirementList);
        Close();
    }
}

/// <summary>
/// Wrapper class for Requirement to use in UI binding.
/// </summary>
public class RequirementWrapper {
    
    public Requirement Requirement { get; }
    
    public RequirementWrapper(Requirement requirement) {
        Requirement = requirement;
    }

    public override string ToString() {
        return Requirement.GetType().Name;
    }
}