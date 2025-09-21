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

using Avalonia;
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
        GotFocus += OnGotFocus;
        
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

// React to scroll changes to update viewport indicator
                scrollViewer.ScrollChanged += (s, _) =>
                {
                    viewModel.UpdateViewportIndicator();
                };
                
                // Initialize viewport and zoom after everything is loaded
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // Apply initial zoom transform first
                    viewModel.UpdateCanvasTransform();
                    // Then center the viewport
                    viewModel.InitializeViewportCenter();
                    // Rebuild visuals to ensure they are present on re-entry
                    viewModel.RebuildSceneVisuals();
                    // Initial indicator update
                    viewModel.UpdateViewportIndicator();
                }, Avalonia.Threading.DispatcherPriority.Background);
            }

            var overlay = this.FindControl<Canvas>("ViewportOverlay");
            if (overlay != null)
            {
                viewModel.SetOverlayCanvas(overlay);
            }
        }
        
        // Set up collapse/expand toggle buttons
        SetupToggleButtons();
        
        // Selection is now handled via direct binding in XAML
        
        // Set focus to enable keyboard input
        Focus();

        // Refresh drop table existence on load
        if (DataContext is ZoneEditorViewModel vm)
        {
            _ = vm.RefreshDropTableCacheAsync();
        }
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

    private async void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is ZoneEditorViewModel vm)
        {
            // When tab regains focus, make sure visuals exist
            vm.RebuildSceneVisuals();
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
        if (collisionObjectsToggle != null && collisionObjectsList != null && collisionObjectsArrow != null)
        {
            collisionObjectsToggle.Click += (s, e) => ToggleSection(collisionObjectsList, collisionObjectsArrow);
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
        
        // Volumes Toggle
        var volumesToggle = this.FindControl<Button>("VolumesToggle");
        var volumesList = this.FindControl<ListBox>("VolumesList");
        var volumesArrow = this.FindControl<TextBlock>("VolumesArrow");
        if (volumesToggle != null && volumesList != null && volumesArrow != null)
        {
            volumesToggle.Click += (s, e) => ToggleSection(volumesList, volumesArrow);
        }
        
        // Triggers Toggle
        var triggersToggle = this.FindControl<Button>("TriggersToggle");
        var triggersList = this.FindControl<ListBox>("TriggersList");
        var triggersArrow = this.FindControl<TextBlock>("TriggersArrow");
        if (triggersToggle != null && triggersList != null && triggersArrow != null)
        {
            triggersToggle.Click += (s, e) => ToggleSection(triggersList, triggersArrow);
        }
    }
    
    private void ToggleSection(ListBox listBox, TextBlock arrow)
    {
        listBox.IsVisible = !listBox.IsVisible;
        arrow.Text = listBox.IsVisible ? "▼" : "►";
    }
    
}
