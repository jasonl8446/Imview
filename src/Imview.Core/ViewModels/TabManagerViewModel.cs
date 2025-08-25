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
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace Imview.Core.ViewModels;

public class TabManagerViewModel : ViewModelBase
{
    private TabItemViewModel? _selectedTab;

    private MainWindowViewModel? _mainWindowViewModel;
    
    public TabManagerViewModel()
    {
        Tabs = new ObservableCollection<TabItemViewModel>();
    }
    
    public void Initialize(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        
        // Add the splash page as the initial non-closable tab
        var splashTab = new TabItemViewModel("Home", new SplashPageViewModel(mainWindowViewModel), canClose: false);
        AddTab(splashTab);
        SelectTab(splashTab);
    }

    public ObservableCollection<TabItemViewModel> Tabs { get; }

    public TabItemViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != null)
                _selectedTab.IsSelected = false;
                
            this.RaiseAndSetIfChanged(ref _selectedTab, value);
            
            if (_selectedTab != null)
                _selectedTab.IsSelected = true;
        }
    }

    public TabItemViewModel AddTab(string title, ViewModelBase content, bool canClose = true)
    {
        var tab = new TabItemViewModel(title, content, canClose);
        AddTab(tab);
        return tab;
    }

    public void AddTab(TabItemViewModel tab)
    {
        tab.CloseRequested += OnTabCloseRequested;
        Tabs.Add(tab);
    }

    public void SelectTab(TabItemViewModel tab)
    {
        if (Tabs.Contains(tab))
        {
            SelectedTab = tab;
        }
    }

    public void CloseTab(TabItemViewModel tab)
    {
        if (!tab.CanClose)
            return;

        tab.CloseRequested -= OnTabCloseRequested;
        
        var wasSelected = tab == SelectedTab;
        var tabIndex = Tabs.IndexOf(tab);
        
        Tabs.Remove(tab);

        if (wasSelected && Tabs.Count > 0)
        {
            // Select the tab to the left, or the first tab if we were at index 0
            var newIndex = Math.Max(0, tabIndex - 1);
            if (newIndex < Tabs.Count)
            {
                SelectTab(Tabs[newIndex]);
            }
        }
    }

    public TabItemViewModel? FindTab(Func<TabItemViewModel, bool> predicate)
    {
        return Tabs.FirstOrDefault(predicate);
    }

    public TabItemViewModel? FindTabByContent<T>() where T : ViewModelBase
    {
        return Tabs.FirstOrDefault(tab => tab.Content is T);
    }

    private void OnTabCloseRequested(object? sender, TabItemViewModel tab)
    {
        CloseTab(tab);
    }
}