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
using System.Windows.Input;
using ReactiveUI;

namespace Imview.Core.ViewModels;

public class TabItemViewModel : ViewModelBase
{
    private string _title;
    private ViewModelBase _content;
    private bool _isSelected;
    private bool _canClose;

    public TabItemViewModel(string title, ViewModelBase content, bool canClose = true)
    {
        _title = title;
        _content = content;
        _canClose = canClose;
        _isSelected = false;
        
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public ViewModelBase Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool CanClose
    {
        get => _canClose;
        set => this.RaiseAndSetIfChanged(ref _canClose, value);
    }

    public event EventHandler<TabItemViewModel>? CloseRequested;

    public ICommand CloseCommand => ReactiveCommand.Create(() => CloseRequested?.Invoke(this, this));
}