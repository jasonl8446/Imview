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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace Imview.Core.ViewModels;

public class WadSelectionDialogViewModel : ViewModelBase
{
    private readonly List<string> _allWads;
    private string _searchText = string.Empty;
    private string? _selectedWad = null;
    private bool _dialogResult = false;

    public WadSelectionDialogViewModel(IEnumerable<string> availableWads)
    {
        _allWads = availableWads.ToList();
        FilteredWads = new ObservableCollection<string>(_allWads);
        
        SelectWadCommand = ReactiveCommand.Create<string>(SelectWad);
        ConfirmSelectionCommand = ReactiveCommand.Create(ConfirmSelection);
        CancelCommand = ReactiveCommand.Create(Cancel);
        
        // Auto-select first WAD if any exist
        if (FilteredWads.Count > 0)
        {
            SelectedWad = FilteredWads[0];
        }
        
        // Set up search filtering
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => FilterWads());
    }

    public ObservableCollection<string> FilteredWads { get; }

    public string DialogTitle => "Select WAD File";
    
    public string SearchPlaceholder => "Search WAD files...";
    
    public string WadCountText => FilteredWads.Count == 1 ? "1 WAD" : $"{FilteredWads.Count} WADs";

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public string? SelectedWad
    {
        get => _selectedWad;
        set => this.RaiseAndSetIfChanged(ref _selectedWad, value);
    }

    public bool DialogResult
    {
        get => _dialogResult;
        private set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    public ICommand SelectWadCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }
    public ICommand CancelCommand { get; }

    private void SelectWad(string wadName)
    {
        SelectedWad = wadName;
    }

    private void ConfirmSelection()
    {
        if (!string.IsNullOrEmpty(SelectedWad))
        {
            DialogResult = true;
        }
    }

    private void Cancel()
    {
        DialogResult = false;
        SelectedWad = null;
    }

    private void FilterWads()
    {
        FilteredWads.Clear();
        
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allWads
            : _allWads.Where(wad => wad.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        
        foreach (var wad in filtered)
        {
            FilteredWads.Add(wad);
        }
        
        // Auto-select first filtered result
        if (FilteredWads.Count > 0 && (string.IsNullOrEmpty(SelectedWad) || !FilteredWads.Contains(SelectedWad)))
        {
            SelectedWad = FilteredWads[0];
        }
        
        this.RaisePropertyChanged(nameof(WadCountText));
    }
}