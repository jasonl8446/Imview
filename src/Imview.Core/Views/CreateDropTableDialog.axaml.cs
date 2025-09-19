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

namespace Imview.Core.Views;

public partial class CreateDropTableDialog : Window {
    public string TableName { get; private set; } = string.Empty;
    public string TableDescription { get; private set; } = string.Empty;
    public bool WasCreated { get; private set; } = false;

    public CreateDropTableDialog() {
        InitializeComponent();
        
        CreateButton.Click += CreateButton_Click;
        CancelButton.Click += CancelButton_Click;
    }

    private void CreateButton_Click(object? sender, RoutedEventArgs e) {
        var name = NameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) {
            // Could add validation feedback here
            return;
        }

        TableName = name;
        TableDescription = DescriptionTextBox.Text?.Trim() ?? string.Empty;
        WasCreated = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) {
        WasCreated = false;
        Close();
    }
}