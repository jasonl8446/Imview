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
using Imview.Core.Database.Models;
using Imview.Core.Services;
using Imview.Core.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Imview.Core.Controls.Results;

public class ResDropTableEditor : EditorWindowBase<ResDropTable> {
    
    private readonly ResDropTable _result;
    private readonly ComboBox _dropTableSelector;
    private readonly NumericUpDown _maxRollsInput;
    private readonly Avalonia.Controls.Button _createNewTableButton;
    private readonly DropTableService _dropTableService;
    private List<DropTable> _availableDropTables = new();

    public ResDropTableEditor(ResDropTable? result = null) 
        : base(result is not null ? "Edit Drop Table Reward" : "Add Drop Table Reward") {
        
        _result = result ?? new ResDropTable();
        _dropTableService = new DropTableService();
        
        Width = EditorConstants.DEFAULT_WINDOW_WIDTH;
        Height = 400;
        Background = EditorConstants.DEFAULT_WINDOW_BACKGROUND;
        
        _dropTableSelector = new ComboBox {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            DisplayMemberBinding = new Avalonia.Data.Binding("Name")
        };
        
        _maxRollsInput = new NumericUpDown {
            Minimum = 0,
            Maximum = int.MaxValue,
            Value = _result.m_maxRolls,
            FormatString = "N0"
        };
        
        _createNewTableButton = new Avalonia.Controls.Button {
            Content = "Create New Drop Table",
            Command = ReactiveCommand.CreateFromTask(CreateNewDropTable)
        };
        
        InitializeComponent();
        _ = LoadDropTablesAsync();
    }
    
    private void InitializeComponent() {
        var dropTablePanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Select Drop Table:" },
                _dropTableSelector,
                _createNewTableButton
            }
        };
        
        var maxRollsPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = "Maximum Rolls:" },
                _maxRollsInput,
                new TextBlock { 
                    Text = "Maximum number of items to roll from the table (0 = no limit)", 
                    FontSize = 12,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
        
        MainPanel.Children.Add(CreateGroupBox("Drop Table Selection", dropTablePanel));
        MainPanel.Children.Add(CreateGroupBox("Roll Settings", maxRollsPanel));
        MainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }
    
    private async Task LoadDropTablesAsync() {
        try {
            _availableDropTables = await _dropTableService.GetAllDropTablesAsync();
            _dropTableSelector.ItemsSource = _availableDropTables;
            
            if (!string.IsNullOrEmpty(_result.m_tableName)) {
                var existingTable = _availableDropTables.FirstOrDefault(t => t.Name == _result.m_tableName);
                if (existingTable != null) {
                    _dropTableSelector.SelectedItem = existingTable;
                }
            }
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to load drop tables: {ex.Message}")
                .Send();
        }
    }
    
    private async Task CreateNewDropTable() {
        try {
            var dialog = new CreateDropTableDialog();
            var result = await dialog.ShowDialog<DropTable?>(this);
            
            if (result != null) {
                await LoadDropTablesAsync();
                _dropTableSelector.SelectedItem = _availableDropTables.FirstOrDefault(t => t.Name == result.Name);
                
                MessageService
                    .Info($"Drop table '{result.Name}' created successfully.")
                    .Send();
            }
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to create drop table: {ex.Message}")
                .Send();
        }
    }
    
    private void Save() {
        try {
            if (_dropTableSelector.SelectedItem is not DropTable selectedTable) {
                MessageService
                    .Error("Please select a drop table.")
                    .Send();
                return;
            }
            
            _result.m_tableName = selectedTable.Name;
            _result.m_maxRolls = (int)(_maxRollsInput.Value ?? 0);
            
            ResultSource.SetResult(_result);
            Close();
        }
        catch (Exception ex) {
            MessageService
                .Error($"Failed to save drop table reward: {ex.Message}")
                .Send();
        }
    }
}