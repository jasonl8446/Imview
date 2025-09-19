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
using Imview.Core.Models;

namespace Imview.Core.Views;

public partial class ZoneEditorView : UserControl
{
    private bool _isDragging = false;
    private Avalonia.Point _lastPointerPosition;

    public ZoneEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        
        // Enable keyboard focus
        Focusable = true;
        
        // Add event handlers
        KeyDown += OnKeyDown;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }
    
    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Wire up the canvas and scroll viewer to the ViewModel
        if (DataContext is ZoneEditorViewModel viewModel)
        {
            var canvas = this.FindControl<Canvas>("ZoneObjectCanvas");
            if (canvas != null)
            {
                viewModel.SetZoneObjectCanvas(canvas);
            }
            
            var scrollViewer = this.FindControl<ScrollViewer>("ZoneScrollViewer");
            if (scrollViewer != null)
            {
                viewModel.SetScrollViewer(scrollViewer);
                
                // Initialize viewport and zoom after everything is loaded
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // Apply initial zoom transform first
                    viewModel.UpdateCanvasTransform();
                    // Then center the viewport
                    viewModel.InitializeViewportCenter();
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
        }
        
        // Set up collapse/expand toggle buttons
        SetupToggleButtons();
        
        // Selection is now handled via direct binding in XAML
        
        // Set focus to enable keyboard input
        Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ZoneEditorViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.W:
                viewModel.PanUpCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.S:
                viewModel.PanDownCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.A:
                viewModel.PanLeftCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.D:
                viewModel.PanRightCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.R:
                viewModel.ResetViewCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.OemPlus:
            case Key.Add:
                viewModel.ZoomInCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.OemMinus:
            case Key.Subtract:
                viewModel.ZoomOutCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Only handle zoom when CTRL is held down
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && DataContext is ZoneEditorViewModel viewModel)
        {
            var canvas = this.FindControl<Canvas>("ZoneCanvas");
            if (canvas != null)
            {
                var position = e.GetPosition(canvas);
                viewModel.HandleMouseWheel(e.Delta.Y, position);
                e.Handled = true;
            }
        }
        // If CTRL is not held, let the ScrollViewer handle normal scrolling
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Only start dragging if the click wasn't handled by a zone object
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && !e.Handled)
        {
            _isDragging = true;
            _lastPointerPosition = e.GetPosition(this);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && DataContext is ZoneEditorViewModel viewModel)
        {
            var currentPosition = e.GetPosition(this);
            var deltaX = currentPosition.X - _lastPointerPosition.X;
            var deltaY = currentPosition.Y - _lastPointerPosition.Y;
            
            viewModel.HandleMouseDrag(deltaX, deltaY);
            
            _lastPointerPosition = currentPosition;
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
    
    private void SetupToggleButtons()
    {
        // Zone Objects Toggle
        var zoneObjectsToggle = this.FindControl<Button>("ZoneObjectsToggle");
        var zoneObjectsList = this.FindControl<ListBox>("ZoneObjectsList");
        var zoneObjectsArrow = this.FindControl<TextBlock>("ZoneObjectsArrow");
        if (zoneObjectsToggle != null && zoneObjectsList != null && zoneObjectsArrow != null)
        {
            zoneObjectsToggle.Click += (s, e) => ToggleSection(zoneObjectsList, zoneObjectsArrow);
        }
        
        // Collision Objects Toggle
        var collisionObjectsToggle = this.FindControl<Button>("CollisionObjectsToggle");
        var collisionObjectsList = this.FindControl<ListBox>("CollisionObjectsList");
        var collisionObjectsArrow = this.FindControl<TextBlock>("CollisionObjectsArrow");
        var collisionFilters = this.FindControl<Border>("CollisionFilters");
        if (collisionObjectsToggle != null && collisionObjectsList != null && collisionObjectsArrow != null && collisionFilters != null)
        {
            collisionObjectsToggle.Click += (s, e) => ToggleSectionWithFilters(collisionObjectsList, collisionObjectsArrow, collisionFilters);
        }
        
        // Paths & Spawns Toggle
        var pathsToggle = this.FindControl<Button>("PathsToggle");
        var pathsList = this.FindControl<ListBox>("PathsList");
        var pathsArrow = this.FindControl<TextBlock>("PathsArrow");
        if (pathsToggle != null && pathsList != null && pathsArrow != null)
        {
            pathsToggle.Click += (s, e) => ToggleSection(pathsList, pathsArrow);
        }
        
        // NIF Geometry Toggle (starts collapsed)
        var nifGeometryToggle = this.FindControl<Button>("NifGeometryToggle");
        var nifGeometryList = this.FindControl<ListBox>("NifGeometryList");
        var nifGeometryArrow = this.FindControl<TextBlock>("NifGeometryArrow");
        if (nifGeometryToggle != null && nifGeometryList != null && nifGeometryArrow != null)
        {
            nifGeometryToggle.Click += (s, e) => ToggleSection(nifGeometryList, nifGeometryArrow);
        }
    }
    
    private void ToggleSection(ListBox listBox, TextBlock arrow)
    {
        listBox.IsVisible = !listBox.IsVisible;
        arrow.Text = listBox.IsVisible ? "▼" : "►";
    }
    
    private void ToggleSectionWithFilters(ListBox listBox, TextBlock arrow, Border filters)
    {
        listBox.IsVisible = !listBox.IsVisible;
        filters.IsVisible = listBox.IsVisible;
        arrow.Text = listBox.IsVisible ? "▼" : "►";
    }
    
}
