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
using Avalonia;
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.ViewModels;
using System;
using System.Reactive.Linq;

namespace Imview.Core.Views;

public partial class ResultDynamicViewerView : UserControl
{
    private ResultDynamicViewerViewModel _viewModel = new();
    
    // Dependency properties for context information
    public static readonly StyledProperty<string?> ZoneNameProperty =
        AvaloniaProperty.Register<ResultDynamicViewerView, string?>(nameof(ZoneName));
        
    public static readonly StyledProperty<string?> TriggerNameProperty =
        AvaloniaProperty.Register<ResultDynamicViewerView, string?>(nameof(TriggerName));
    
    public string? ZoneName
    {
        get => GetValue(ZoneNameProperty);
        set => SetValue(ZoneNameProperty, value);
    }
    
    public string? TriggerName
    {
        get => GetValue(TriggerNameProperty);
        set => SetValue(TriggerNameProperty, value);
    }
    
    public ResultDynamicViewerView()
    {
        InitializeComponent();
        
        // Monitor property changes
        this.GetObservable(ZoneNameProperty).Subscribe(zoneName => {
            _viewModel.ZoneName = zoneName;
        });
        this.GetObservable(TriggerNameProperty).Subscribe(triggerName => {
            _viewModel.TriggerName = triggerName;
        });
        
        // Monitor DataContext changes for the Result data
        this.GetObservable(DataContextProperty).Subscribe(dataContext =>
        {
            if (dataContext is Result result)
            {
                _viewModel.Result = result;
            }
        });
    }

    public ResultDynamicViewerView(Result result) : this()
    {
        // Set the result as DataContext
        DataContext = result;
    }

    public ResultDynamicViewerViewModel ViewModel => _viewModel;
    
    private void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Avalonia.Controls.Button button && button.Tag is PropertyNode node)
        {
            _viewModel.EditTeleportCommand.Execute(node).Subscribe();
        }
    }
}
