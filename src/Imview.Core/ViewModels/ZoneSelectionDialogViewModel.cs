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

public class ZoneSelectionDialogViewModel : ViewModelBase
{
    private readonly List<string> _allZones;
    private string _searchText = string.Empty;
    private string? _selectedZone = null;
    private bool _dialogResult = false;

    public ZoneSelectionDialogViewModel(IEnumerable<string> availableZones)
    {
        _allZones = availableZones.ToList();
        FilteredZones = new ObservableCollection<string>(_allZones);
        
        SelectZoneCommand = ReactiveCommand.Create<string>(SelectZone);
        ConfirmSelectionCommand = ReactiveCommand.Create(ConfirmSelection);
        CancelCommand = ReactiveCommand.Create(Cancel);
        
        // Auto-select first zone if any exist
        if (FilteredZones.Count > 0)
        {
            SelectedZone = FilteredZones[0];
        }
    }

    public ObservableCollection<string> FilteredZones { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterZones();
        }
    }

    public string? SelectedZone
    {
        get => _selectedZone;
        set => this.RaiseAndSetIfChanged(ref _selectedZone, value);
    }

    public bool DialogResult
    {
        get => _dialogResult;
        private set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    public bool HasSelectedZone => !string.IsNullOrEmpty(SelectedZone);
    public string DialogTitle => "Select Zone";
    public string SearchPlaceholder => "Search zones...";
    public int TotalZoneCount => _allZones.Count;
    public int FilteredZoneCount => FilteredZones.Count;
    public string ZoneCountText => FilteredZoneCount == TotalZoneCount 
        ? $"{TotalZoneCount} zones" 
        : $"{FilteredZoneCount} of {TotalZoneCount} zones";

    public ICommand SelectZoneCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }
    public ICommand CancelCommand { get; }

    private void FilterZones()
    {
        FilteredZones.Clear();
        
        var filteredZones = string.IsNullOrWhiteSpace(SearchText)
            ? _allZones
            : _allZones.Where(zone => zone.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        
        foreach (var zone in filteredZones)
        {
            FilteredZones.Add(zone);
        }
        
        // Auto-select first zone after filtering
        if (FilteredZones.Count > 0 && !FilteredZones.Contains(SelectedZone))
        {
            SelectedZone = FilteredZones[0];
        }
        else if (FilteredZones.Count == 0)
        {
            SelectedZone = null;
        }
        
        // Update computed properties
        this.RaisePropertyChanged(nameof(FilteredZoneCount));
        this.RaisePropertyChanged(nameof(ZoneCountText));
        this.RaisePropertyChanged(nameof(HasSelectedZone));
    }

    private void SelectZone(string zoneName)
    {
        SelectedZone = zoneName;
        this.RaisePropertyChanged(nameof(HasSelectedZone));
    }

    private void ConfirmSelection()
    {
        if (HasSelectedZone)
        {
            DialogResult = true;
        }
    }

    private void Cancel()
    {
        DialogResult = false;
        SelectedZone = null;
    }

    public void HandleDoubleClick(string zoneName)
    {
        SelectedZone = zoneName;
        ConfirmSelection();
    }
}