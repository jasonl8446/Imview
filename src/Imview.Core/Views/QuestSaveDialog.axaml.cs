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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Imview.Core.Services;

namespace Imview.Core.Views;

public partial class QuestSaveDialog : Window {
    
    public QuestSaveDialog(string questName = "", string description = "") {
        InitializeComponent();
        
        QuestNameTextBox.Text = questName;
        QuestDescriptionTextBox.Text = description;
        
        // Focus on name textbox
        QuestNameTextBox.Focus();
        
        // Select all text if there's existing content
        if (!string.IsNullOrEmpty(questName)) {
            QuestNameTextBox.SelectAll();
        }
    }
    
    private void OnSaveClick(object? sender, RoutedEventArgs e) {
        var name = QuestNameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) {
            // Show error - name is required
            return;
        }
        
        var description = QuestDescriptionTextBox.Text?.Trim() ?? "";
        
        var result = new QuestSaveResult(name, description);
        Close(result);
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e) {
        Close(null);
    }
}