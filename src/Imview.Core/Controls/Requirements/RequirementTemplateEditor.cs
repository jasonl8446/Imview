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
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Imcodec.IO;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.Types;
using Imview.Core.Common.Constants;
using Imview.Core.Common.Extensions;
using Imview.Core.Common.Utils;
using Imview.Core.Controls.Base;
using Imview.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Imview.Core.Controls.Requirements;

/// <summary>
/// Editor for Requirement templates used in quest requirements.
/// </summary>
public class RequirementTemplateEditor : EditorWindowBase<Requirement> {

    // Core properties.
    private readonly Requirement _template;
    private readonly Dictionary<string, Control> _propertyEditors = [];
    
    // UI Controls.
    private readonly ComboBox _typeSelector;
    private readonly StackPanel _propertyPanel;
    private string _selectedRequirementType;
    
    // Services.
    private readonly IReadOnlyDictionary<string, System.Type> _requirementTypes;

    public RequirementTemplateEditor(Requirement? template = null, IReadOnlyDictionary<string, System.Type>? requirementTypes = null) 
        : base(template is not null ? "Edit Requirement" : "Add Requirement") {
        
        _template = template ?? new Requirement();
        _requirementTypes = requirementTypes ?? RequirementFinderService.RequirementTypes;
        _selectedRequirementType = string.Empty;
        
        Width = EditorConstants.SMALL_WINDOW_WIDTH;
        Height = EditorConstants.SMALL_WINDOW_HEIGHT;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _typeSelector = new ComboBox {
            Width = EditorConstants.DEFAULT_SELECTOR_WIDTH,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // Bugfix: Virtualization causes the ComboBox to grow/shrink as items are shown/hidden.
        // https://github.com/AvaloniaUI/Avalonia/issues/11018#issuecomment-1510803095
        _typeSelector.ItemsPanel = new FuncTemplate<Panel?>(new(() => new StackPanel()));
        
        _propertyPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING
        };
        
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        // Populate type selector with requirement types.
        _typeSelector.ItemsSource = GetRequirementTypeNames();

        // If editing existing requirement, select its type.
        if (_template != null && _template.GetType() != typeof(Requirement)) {
            var typeName = _template.GetType().Name;
            _typeSelector.SelectedItem = typeName;
            _selectedRequirementType = typeName;
            _typeSelector.IsEnabled = false; // Don't allow type changes for existing requirements.
        }
        
        _typeSelector.SelectionChanged += TypeSelector_SelectionChanged;
        
        var typeSelectorContent = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Select Requirement Type:" },
                _typeSelector
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Requirement Type", typeSelectorContent));
        
        // Property panel.
        var propertyScroller = new ScrollViewer {
            Content = _propertyPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        
        MainPanel.Children.Add(CreateGroupBox("Properties", propertyScroller));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
        
        // If editing existing requirement, initialize properties.
        if (_template != null && _template.GetType() != typeof(Requirement)) {
            PopulatePropertyEditors(_template.GetType());
            InitializeValues();
        }
    }

    private IEnumerable<string> GetRequirementTypeNames() 
        => _requirementTypes.Keys
            .Where(typeName => typeName.StartsWith("Req") && !typeName.StartsWith("Requirement"))
            .OrderBy(t => t);

    private void TypeSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (_typeSelector.SelectedItem is string typeName) {
            _selectedRequirementType = typeName;
            var type = _requirementTypes.TryGetValue(_selectedRequirementType, out var t) ? t : null;
            
            if (type != null) {
                PopulatePropertyEditors(type);
            }
        }
    }
    
    private void PopulatePropertyEditors(System.Type requirementType) {
        _propertyPanel.Children.Clear();
        _propertyEditors.Clear();
        
        var properties = requirementType.GetEditableProperties()
            .OrderBy(p => p.Name);
        
        foreach (var property in properties) {
            var editor = CreateEditorForProperty(property);
            _propertyPanel.Children.Add(editor);
        }
    }
    
    private Control CreateEditorForProperty(PropertyInfo property) {
        var displayName = property.GetDisplayName();
        var propertyType = property.PropertyType;
        
        Control? editor = null;
        
        // Create appropriate editor based on property type.
        if (propertyType == typeof(bool)) {
            editor = new CheckBox();
            _propertyEditors[property.Name] = editor;
        }
        else if (propertyType.IsEnum) {
            var comboBox = new ComboBox {
                ItemsSource = Enum.GetValues(propertyType)
            };
            _propertyEditors[property.Name] = comboBox;
            editor = comboBox;
        }
        else if (propertyType.IsCollectionType()) {
            var textBox = new TextBox {
                Watermark = "Comma-separated values"
            };
            _propertyEditors[property.Name] = textBox;
            editor = textBox;
        }
        else if (propertyType == typeof(string) || 
                 propertyType == typeof(ByteString) ||
                 propertyType == typeof(WideByteString) ||
                 propertyType == typeof(GID) ||
                 propertyType.IsPrimitive) {
            
            var textBox = new TextBox();
            _propertyEditors[property.Name] = textBox;
            editor = textBox;
        }
        else if (propertyType == typeof(RequirementList) || typeof(RequirementList).IsAssignableFrom(propertyType)) {
            // Special handling for nested RequirementList
            var currentList = property.GetValue(_template) as RequirementList;
            var requirementCount = currentList?.m_requirements?.Count ?? 0;
            var button = new Avalonia.Controls.Button {
                Content = $"Edit {displayName} ({requirementCount} requirements)",
                Command = ReactiveCommand.Create(() => EditRequirementList(property))
            };
            _propertyEditors[property.Name] = button;
            editor = button;
        }
        else {
            // Complex type case
            var button = new Avalonia.Controls.Button {
                Content = $"Edit {displayName}",
                Command = ReactiveCommand.Create(() => EditComplexProperty(property))
            };
            _propertyEditors[property.Name] = button;
            editor = button;
        }
        
        // Add validation hint for numeric and other types.
        var hint = ValueConverters.GetTypeValidationHint(propertyType);
        
        var container = new StackPanel {
            Spacing = 5,
            Margin = new Thickness(0, 0, 0, 5)
        };
        
        container.Children.Add(new TextBlock { Text = displayName });
        
        if (editor != null) {
            container.Children.Add(editor);
        }
        
        if (!string.IsNullOrEmpty(hint)) {
            container.Children.Add(new TextBlock {
                Text = hint,
                FontSize = 12,
                Foreground = Brushes.Gray,
                FontStyle = FontStyle.Italic
            });
        }
        
        return container;
    }

    private async void EditRequirementList(PropertyInfo property) {
        // Get current value
        var currentValue = property.GetValue(_template) as RequirementList;
        
        // Launch nested RequirementListEditor
        var factory = new RequirementEditorFactory();
        var editor = new RequirementListEditor(currentValue, factory);
        
        await editor.ShowDialog(this);
        var result = await editor.GetResultAsync();
        
        if (result != null) {
            // Update the property value
            property.SetValue(_template, result);
            
            // Update the button text to indicate it has been edited
            if (_propertyEditors.TryGetValue(property.Name, out var button) && button is Avalonia.Controls.Button btn) {
                var requirementCount = result.m_requirements?.Count ?? 0;
                btn.Content = $"Edit {property.GetDisplayName()} ({requirementCount} requirements)";
            }
        }
    }
    
    private async void EditComplexProperty(PropertyInfo property) {
        // For complex properties, we'd typically launch a nested editor.
        // This is a placeholder for complex property editing.
        MessageService
            .Info("Complex editor is not yet implemented.")
            .WithDuration(TimeSpan.FromSeconds(5))
            .Send();
    }

    private void InitializeValues() {
        if (_template == null) {
            return;
        }

        foreach (var property in _template.GetType().GetEditableProperties()) {
            if (_propertyEditors.TryGetValue(property.Name, out var control)) {
                var value = property.GetValue(_template);
                
                switch (control) {
                    case CheckBox checkbox:
                        checkbox.IsChecked = value as bool? ?? false;
                        break;
                        
                    case ComboBox comboBox:
                        comboBox.SelectedItem = value;
                        break;
                        
                    case TextBox textBox:
                        if (property.PropertyType.IsCollectionType()) {
                            textBox.Text = value is IEnumerable<object> collection
                                ? string.Join(", ", collection)
                                : string.Empty;
                        }
                        else {
                            textBox.Text = value?.ToString() ?? string.Empty;
                        }
                        break;
                }
            }
        }
    }
    
    private void Save() {
        try {
            if (string.IsNullOrEmpty(_selectedRequirementType)) {
                MessageService
                    .Error("Please select a requirement type.")
                    .Send();

                return;
            }
            
            // Get the type and create an instance if needed
            if (!_requirementTypes.TryGetValue(_selectedRequirementType, out var requirementType)) {
                MessageService
                    .Error("Invalid requirement type selected.")
                    .Send();

                return;
            }
            
            // Create or use existing template
            var requirement = _template?.GetType() == requirementType 
                ? _template 
                : (Requirement)Activator.CreateInstance(requirementType)!;
            
            // Update all properties
            foreach (var property in requirementType.GetEditableProperties()) {
                if (_propertyEditors.TryGetValue(property.Name, out var editor)) {
                    object? value = null;
                    
                    switch (editor) {
                        case CheckBox checkbox:
                            value = checkbox.IsChecked ?? false;
                            break;
                            
                        case ComboBox comboBox:
                            value = comboBox.SelectedItem;
                            break;
                            
                        case TextBox textBox:
                            try {
                                value = ValueConverters.ConvertValue(textBox.Text ?? string.Empty, property.PropertyType);
                            }
                            catch (Exception ex) {
                                MessageService
                                    .Error($"Invalid value for {property.Name}: {ex.Message}")
                                    .Send();

                                return;
                            }
                            break;
                    }
                    
                    if (value != null || Nullable.GetUnderlyingType(property.PropertyType) != null) {
                        property.SetValue(requirement, value);
                    }
                }
            }
            
            ResultSource.SetResult(requirement);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save requirement: {ex.Message}")
                .Send();
        }
    }

}