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
using Avalonia.Input;
using Avalonia.Interactivity;
using Imview.Core.ViewModels;
using System.Reactive.Linq;
using System;
using ReactiveUI;

namespace Imview.Core.Views;

public partial class ZoneSelectionDialog : Window
{
    public ZoneSelectionDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        
        // Handle keyboard shortcuts
        KeyDown += OnKeyDown;
        
        // Handle double-click on list items
        var listBox = this.FindControl<ListBox>("listBox");
        if (listBox != null)
        {
            listBox.DoubleTapped += OnListBoxDoubleTapped;
        }
    }

    public ZoneSelectionDialog(ZoneSelectionDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Subscribe to dialog result changes
        if (viewModel != null)
        {
            viewModel.WhenAnyValue(x => x.DialogResult)
                .Where(result => result)
                .Subscribe(_ => Close(viewModel.SelectedZone));
                
            viewModel.WhenAnyValue(x => x.DialogResult)
                .Where(result => !result && viewModel.SelectedZone == null)
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
        if (DataContext is not ZoneSelectionDialogViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                if (viewModel.HasSelectedZone)
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

    private void OnListBoxDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ZoneSelectionDialogViewModel viewModel && 
            sender is ListBox listBox && 
            listBox.SelectedItem is string selectedZone)
        {
            viewModel.HandleDoubleClick(selectedZone);
        }
    }

    public static async System.Threading.Tasks.Task<string?> ShowDialogAsync(Window parent, ZoneSelectionDialogViewModel viewModel)
    {
        var dialog = new ZoneSelectionDialog(viewModel);
        var result = await dialog.ShowDialog<string?>(parent);
        return result;
    }
}