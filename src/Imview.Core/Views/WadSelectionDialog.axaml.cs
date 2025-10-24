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

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Imview.Core.ViewModels;
using ReactiveUI;

namespace Imview.Core.Views;

public partial class WadSelectionDialog : Window
{
    public WadSelectionDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        
        // Handle keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    public WadSelectionDialog(WadSelectionDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Subscribe to dialog result changes
        if (viewModel != null)
        {
            viewModel.WhenAnyValue(x => x.DialogResult)
                .Where(result => result)
                .Subscribe(_ => Close(viewModel.SelectedWad));
                
            viewModel.WhenAnyValue(x => x.DialogResult)
                .Where(result => !result && viewModel.SelectedWad == null)
                .Subscribe(_ => Close(null));
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Focus the search textbox for immediate typing
        var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
        searchTextBox?.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not WadSelectionDialogViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                if (!string.IsNullOrEmpty(viewModel.SelectedWad))
                {
                    viewModel.ConfirmSelectionCommand.Execute(null);
                }
                e.Handled = true;
                break;
                
            case Key.Escape:
                viewModel.CancelCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Shows the WAD selection dialog and returns the selected WAD name.
    /// </summary>
    /// <param name="parent">Parent window</param>
    /// <param name="viewModel">Dialog view model</param>
    /// <returns>Selected WAD name or null if cancelled</returns>
    public static async Task<string?> ShowDialogAsync(Window parent, WadSelectionDialogViewModel viewModel)
    {
        var dialog = new WadSelectionDialog(viewModel);
        var result = await dialog.ShowDialog<string?>(parent);
        return result;
    }
}