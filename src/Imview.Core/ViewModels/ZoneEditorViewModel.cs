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
using System.Windows.Input;
using System.Threading.Tasks;
using System.Linq;
using ReactiveUI;
using Avalonia.Controls;
using Avalonia.Media;
using Imview.Core.Services;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.Math;

namespace Imview.Core.ViewModels;

public class ZoneEditorViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly ZoneDataService _zoneDataService;
    private string _selectedZone = string.Empty;
    private ZoneVisualizationObject? _selectedObject = null;
    private CoreObjectInfo? _selectedCoreObject = null;
    private WizZoneData? _currentZoneData = null;
    private bool _isLoading = false;
    private Canvas? _zoneObjectCanvas = null;

    public ZoneEditorViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _zoneDataService = new ZoneDataService();
        
        LoadZoneCommand = ReactiveCommand.Create(LoadZone);
        SaveZoneCommand = ReactiveCommand.Create(SaveZone);
        SelectObjectCommand = ReactiveCommand.Create<ZoneVisualizationObject>(SelectObject);
        
        Zones = new ObservableCollection<string>();
        ZoneObjects = new ObservableCollection<ZoneObjectItem>();
        Shopkeepers = new ObservableCollection<ShopkeeperItem>();
        ZoneTransfers = new ObservableCollection<ZoneTransferItem>();
        ZoneVisualizationObjects = new ObservableCollection<ZoneVisualizationObject>();
        
        LoadAvailableZones();
    }

    public ObservableCollection<string> Zones { get; }
    public ObservableCollection<ZoneObjectItem> ZoneObjects { get; }
    public ObservableCollection<ShopkeeperItem> Shopkeepers { get; }
    public ObservableCollection<ZoneTransferItem> ZoneTransfers { get; }
    public ObservableCollection<ZoneVisualizationObject> ZoneVisualizationObjects { get; }

    public string SelectedZone
    {
        get => _selectedZone;
        set => this.RaiseAndSetIfChanged(ref _selectedZone, value);
    }

    public ZoneVisualizationObject? SelectedObject
    {
        get => _selectedObject;
        set => this.RaiseAndSetIfChanged(ref _selectedObject, value);
    }

    public CoreObjectInfo? SelectedCoreObject
    {
        get => _selectedCoreObject;
        set => this.RaiseAndSetIfChanged(ref _selectedCoreObject, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool HasSelectedObject => SelectedObject != null;

    // Expose Vector3 components for XAML binding
    public float LocationX => SelectedCoreObject?.m_location.X ?? 0f;
    public float LocationY => SelectedCoreObject?.m_location.Y ?? 0f;  
    public float LocationZ => SelectedCoreObject?.m_location.Z ?? 0f;
    public float OrientationX => SelectedCoreObject?.m_orientation.X ?? 0f;
    public float OrientationY => SelectedCoreObject?.m_orientation.Y ?? 0f;
    public float OrientationZ => SelectedCoreObject?.m_orientation.Z ?? 0f;

    public ICommand LoadZoneCommand { get; }
    public ICommand SaveZoneCommand { get; }
    public ICommand SelectObjectCommand { get; }
    
    public void SetZoneObjectCanvas(Canvas canvas)
    {
        _zoneObjectCanvas = canvas;
    }

    private async void LoadZone()
    {
        if (string.IsNullOrEmpty(SelectedZone))
        {
            MessageService.Info("Please select a zone to load.")
                .WithDuration(TimeSpan.FromSeconds(3))
                .Send();
            return;
        }

        try
        {
            IsLoading = true;
            MessageService.Info($"Loading zone: {SelectedZone}")
                .WithDuration(TimeSpan.FromSeconds(2))
                .Send();

            _currentZoneData = await _zoneDataService.LoadZoneDataAsync(SelectedZone);
            if (_currentZoneData != null)
            {
                await PopulateZoneData(_currentZoneData);
                MessageService.Info($"Zone '{SelectedZone}' loaded successfully")
                    .WithDuration(TimeSpan.FromSeconds(2))
                    .Send();
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load zone: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SaveZone()
    {
        if (string.IsNullOrEmpty(SelectedZone))
        {
            MessageService.Info("No zone loaded to save.")
                .WithDuration(TimeSpan.FromSeconds(3))
                .Send();
            return;
        }

        MessageService.Info($"Zone saving functionality coming soon...")
            .WithDuration(TimeSpan.FromSeconds(3))
            .Send();
    }

    private async void LoadAvailableZones()
    {
        try
        {
            var zones = await _zoneDataService.GetAvailableZonesAsync();
            Zones.Clear();
            foreach (var zone in zones)
            {
                Zones.Add(zone);
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load available zones: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
        }
    }

    private async Task PopulateZoneData(WizZoneData zoneData)
    {
        // Process zone data on background thread, but update UI collections on UI thread
        var tempZoneObjects = new List<ZoneObjectItem>();
        var tempShopkeepers = new List<ShopkeeperItem>();
        var tempZoneTransfers = new List<ZoneTransferItem>();
        var tempVisualizationObjects = new List<ZoneVisualizationObject>();
        
        await Task.Run(() =>
        {
            Console.WriteLine($"PopulateZoneData called with zone: {zoneData?.m_zoneName}");
            Console.WriteLine($"Object list is null: {zoneData?.m_objectList == null}");
            Console.WriteLine($"Object list count: {zoneData?.m_objectList?.Count ?? 0}");

            // Process zone objects
            if (zoneData.m_objectList != null)
            {
                Console.WriteLine($"Processing {zoneData.m_objectList.Count} objects from zone data");
                var validObjects = zoneData.m_objectList.Where(o => o != null).ToList();
                Console.WriteLine($"Found {validObjects.Count} non-null objects");
                
                foreach (var obj in validObjects)
                {
                    // Add to general objects list
                    var zoneObjectItem = new ZoneObjectItem
                    {
                        Name = obj.m_zoneTag ?? $"Object_{obj.m_templateID}",
                        Type = DetermineObjectType(obj),
                        TemplateID = (ulong)obj.m_templateID,
                        Location = obj.m_location,
                        Scale = obj.m_fScale
                    };
                    tempZoneObjects.Add(zoneObjectItem);

                    // Add to visualization
                    var visualObj = new ZoneVisualizationObject
                    {
                        Name = zoneObjectItem.Name,
                        Type = zoneObjectItem.Type,
                        X = obj.m_location.X,
                        Y = obj.m_location.Y,
                        Z = obj.m_location.Z,
                        Scale = obj.m_fScale,
                        TemplateID = (ulong)obj.m_templateID
                    };
                    
                    // Debug: Log object coordinates
                    if (tempVisualizationObjects.Count < 10) // Only log first 10 objects to avoid spam
                    {
                        Console.WriteLine($"Object: {visualObj.Name} at ({visualObj.X:F1}, {visualObj.Y:F1}, {visualObj.Z:F1}) - Type: {visualObj.Type}");
                    }
                    
                    tempVisualizationObjects.Add(visualObj);

                    // Add to shopkeepers if applicable
                    if (IsShopkeeper(obj))
                    {
                        var shopkeeper = new ShopkeeperItem
                        {
                            Name = obj.m_zoneTag ?? "Unknown Shopkeeper",
                            TemplateID = (ulong)obj.m_templateID,
                            InventoryCount = 0 // Would need to query database for actual count
                        };
                        tempShopkeepers.Add(shopkeeper);
                    }
                }
            }

            // Process teleports (mock data for now)
            tempZoneTransfers.Add(new ZoneTransferItem { TriggerName = "ZoneExit1", DestinationZone = "Commons" });
            tempZoneTransfers.Add(new ZoneTransferItem { TriggerName = "ZoneExit2", DestinationZone = "Ravenwood" });
            
            Console.WriteLine($"Zone processing complete. Temp collection counts:");
            Console.WriteLine($"  tempVisualizationObjects: {tempVisualizationObjects.Count}");
            Console.WriteLine($"  tempZoneObjects: {tempZoneObjects.Count}");
            Console.WriteLine($"  tempShopkeepers: {tempShopkeepers.Count}");
        });
        
        // Now update the UI collections on the UI thread
        Console.WriteLine($"Updating UI collections on UI thread...");
        
        // Clear existing data
        ZoneObjects.Clear();
        Shopkeepers.Clear();
        ZoneTransfers.Clear();
        ZoneVisualizationObjects.Clear();
        
        // Add all items to UI collections
        foreach (var item in tempZoneObjects)
            ZoneObjects.Add(item);
            
        foreach (var item in tempShopkeepers)
            Shopkeepers.Add(item);
            
        foreach (var item in tempZoneTransfers)
            ZoneTransfers.Add(item);
            
        foreach (var item in tempVisualizationObjects)
            ZoneVisualizationObjects.Add(item);
            
        Console.WriteLine($"UI collections updated. Final counts:");
        Console.WriteLine($"  ZoneVisualizationObjects: {ZoneVisualizationObjects.Count}");
        Console.WriteLine($"  ZoneObjects: {ZoneObjects.Count}");
        Console.WriteLine($"  Shopkeepers: {Shopkeepers.Count}");
        
        // Now create visual objects on the canvas
        CreateVisualObjects();
    }

    private void CreateVisualObjects()
    {
        if (_zoneObjectCanvas == null)
        {
            Console.WriteLine("Canvas not set, cannot create visual objects");
            return;
        }
        
        Console.WriteLine($"Creating visual objects on canvas for {ZoneVisualizationObjects.Count} objects");
        
        // Clear existing objects
        _zoneObjectCanvas.Children.Clear();
        
        foreach (var obj in ZoneVisualizationObjects)
        {
            try
            {
                // Convert coordinates
                var canvasX = 10000.0 + (obj.X * 0.25);
                var canvasY = 10000.0 - (obj.Y * 0.25);
                
                // Create visual element
                var border = new Border
                {
                    Background = GetObjectColorBrush(obj.Type),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Avalonia.Thickness(1),
                    CornerRadius = new Avalonia.CornerRadius(3),
                    Width = 16,
                    Height = 16
                };
                
                var textBlock = new TextBlock
                {
                    Text = GetObjectIcon(obj.Type),
                    FontSize = 10,
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                
                border.Child = textBlock;
                
                // Position on canvas
                Canvas.SetLeft(border, canvasX);
                Canvas.SetTop(border, canvasY);
                
                // Add tooltip
                ToolTip.SetTip(border, $"{obj.Name} ({obj.Type}) at ({obj.X:F1}, {obj.Y:F1})");
                
                // Add to canvas
                _zoneObjectCanvas.Children.Add(border);
                
                // Debug for first few objects
                if (_zoneObjectCanvas.Children.Count <= 5)
                {
                    Console.WriteLine($"Added object '{obj.Name}' at canvas position ({canvasX:F1}, {canvasY:F1})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating visual for object '{obj.Name}': {ex.Message}");
            }
        }
        
        Console.WriteLine($"Created {_zoneObjectCanvas.Children.Count} visual objects on canvas");
    }

    private void SelectObject(ZoneVisualizationObject visualObj)
    {
        SelectedObject = visualObj;
        
        // Find the corresponding CoreObjectInfo
        if (_currentZoneData?.m_objectList != null)
        {
            SelectedCoreObject = _currentZoneData.m_objectList
                .FirstOrDefault(obj => obj != null && (ulong)obj.m_templateID == visualObj.TemplateID);
        }
        
        // Notify UI of property changes
        this.RaisePropertyChanged(nameof(HasSelectedObject));
        this.RaisePropertyChanged(nameof(LocationX));
        this.RaisePropertyChanged(nameof(LocationY));
        this.RaisePropertyChanged(nameof(LocationZ));
        this.RaisePropertyChanged(nameof(OrientationX));
        this.RaisePropertyChanged(nameof(OrientationY));
        this.RaisePropertyChanged(nameof(OrientationZ));
    }

    private string DetermineObjectType(CoreObjectInfo obj)
    {
        var name = obj.m_zoneTag?.ToLower() ?? "";

        // Use Dragon tool logic: Check if it's a volume/trigger first
        if (IsVolume(obj)) return "Volume";
        
        // Check for shopkeepers using Dragon NPC tool logic
        if (IsShopkeeper(obj)) return "Shopkeeper"; 
        
        // Check for professors (similar to shopkeeper detection)
        if (IsProfessor(obj)) return "Professor";
        
        // Check for generic NPCs
        if (IsNPC(obj)) return "NPC";
        
        // Check for buildings
        if (name.Contains("building") || name.Contains("house") || name.Contains("school")) return "Building";
        
        return "Object";
    }

    private bool IsVolume(CoreObjectInfo obj)
    {
        // Volumes are objects that create collision/trigger areas
        var name = obj.m_zoneTag?.ToLower() ?? "";
        return name.Contains("volume") || 
               name.Contains("trigger") || 
               name.Contains("area") ||
               name.Contains("zone") ||
               name.Contains("teleport") ||
               name.Contains("portal");
    }

    private bool IsShopkeeper(CoreObjectInfo obj)
    {
        // Following Dragon NPC tool logic from FindShopSuspectObjects
        var name = obj.m_zoneTag?.ToLower() ?? "";
        return name.Contains("shop") || name.Contains("npc");
    }

    private bool IsProfessor(CoreObjectInfo obj)
    {
        var name = obj.m_zoneTag?.ToLower() ?? "";
        return name.Contains("professor") || 
               name.Contains("teacher") || 
               name.Contains("trainer") ||
               name.Contains("instructor");
    }

    private bool IsNPC(CoreObjectInfo obj)
    {
        var name = obj.m_zoneTag?.ToLower() ?? "";
        // Generic NPC detection - any object with a character-like name that isn't a shopkeeper/professor
        return !string.IsNullOrEmpty(name) && 
               !IsShopkeeper(obj) && 
               !IsProfessor(obj) && 
               !IsVolume(obj) &&
               (name.Contains("character") || 
                name.Contains("guard") || 
                name.Contains("citizen") ||
                char.IsUpper(name[0])); // Names starting with uppercase are likely NPCs
    }
    
    private IBrush GetObjectColorBrush(string objectType)
    {
        return objectType switch
        {
            "NPC" => new SolidColorBrush(Avalonia.Media.Color.FromRgb(100, 150, 255)),      // Light blue
            "Shopkeeper" => new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 200, 50)), // Gold
            "Professor" => new SolidColorBrush(Avalonia.Media.Color.FromRgb(150, 255, 150)), // Light green
            "Building" => new SolidColorBrush(Avalonia.Media.Color.FromRgb(150, 100, 50)),   // Brown
            "Volume" => new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 100, 100)),    // Red
            "Object" => new SolidColorBrush(Avalonia.Media.Color.FromRgb(150, 150, 150)),    // Gray
            _ => new SolidColorBrush(Avalonia.Media.Color.FromRgb(100, 100, 100))            // Dark gray
        };
    }
    
    private string GetObjectIcon(string objectType)
    {
        return objectType switch
        {
            "NPC" => "N",
            "Shopkeeper" => "$",
            "Professor" => "P",
            "Building" => "■",
            "Volume" => "○",
            "Object" => "•",
            _ => "?"
        };
    }
}

public class ZoneObjectItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ulong TemplateID { get; set; }
    public Vector3 Location { get; set; }
    public float Scale { get; set; } = 1.0f;
}

public class ZoneVisualizationObject
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ulong TemplateID { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Scale { get; set; } = 1.0f;
    
    public string DisplayText => $"{Name} ({Type})";
    public string CoordinateText => $"({X:F1}, {Y:F1}, {Z:F1})";
}

public class ShopkeeperItem
{
    public string Name { get; set; } = string.Empty;
    public ulong TemplateID { get; set; }
    public int InventoryCount { get; set; }
}

public class ZoneTransferItem
{
    public string TriggerName { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
}