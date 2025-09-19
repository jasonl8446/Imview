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
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Imview.Core.Services;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.Math;
using Imcodec.BCD;
using BcdGeomParams = Imcodec.BCD.GeomParams;
using WizardTea.Core;

namespace Imview.Core.ViewModels;

public class ZoneEditorViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly ZoneDataService _zoneDataService;
    private string _selectedZone = string.Empty;
    private ZoneVisualizationObject? _selectedObject = null;
    private CoreObjectInfo? _selectedCoreObject = null;
    private WizZoneData? _currentZoneData = null;
    private Bcd? _currentCollisionData = null;
    private NifFile? _currentSceneFile = null;
    private SpawnManager? _currentSpawnData = null;
    private PathTemplateList? _currentPathData = null;
    private NodeTemplateList? _currentNodeData = null;
    private NifGeometryProcessor? _nifProcessor = null;
    private bool _isLoading = false;
    private bool _showCollisions = true;
    private bool _showZoneObjects = true;
    private bool _showNifGeometry = true;
    private bool _showSpawns = true;
    private bool _showPaths = true;
    private bool _showNodes = true; // Make visible by default for debugging
    
    // Collision shape filters - default to true so all shapes show initially
    private bool _showBoxCollisions = true;
    private bool _showSphereCollisions = true;
    private bool _showCylinderCollisions = true;
    private bool _showTubeCollisions = true;
    private bool _showPlaneCollisions = true;
    private bool _showMeshCollisions = true;
    private bool _showRayCollisions = true;
    
    private Canvas? _zoneObjectCanvas = null;
    
    // Viewport state for camera control
    private double _zoomLevel = 1.0;
    private double _panX = 0.0;
    private double _panY = 0.0;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 5.0;
    private const double ZoomStep = 0.1;
    private const double PanStep = 50.0;
    private const double CanvasCenterX = 10000.0;
    private const double CanvasCenterY = 10000.0;

    public ZoneEditorViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _zoneDataService = new ZoneDataService();
        
        LoadZoneCommand = ReactiveCommand.Create(LoadZone);
        SaveZoneCommand = ReactiveCommand.Create(SaveZone);
        SelectObjectCommand = ReactiveCommand.Create<ZoneVisualizationObject>(SelectObject);
        SelectZoneCommand = ReactiveCommand.Create(SelectZone);
        
        // Initialize viewport commands
        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
        ResetViewCommand = ReactiveCommand.Create(ResetView);
        PanUpCommand = ReactiveCommand.Create(PanUp);
        PanDownCommand = ReactiveCommand.Create(PanDown);
        PanLeftCommand = ReactiveCommand.Create(PanLeft);
        PanRightCommand = ReactiveCommand.Create(PanRight);
        
        Zones = new ObservableCollection<string>();
        ZoneObjects = new ObservableCollection<ZoneObjectItem>();
        Shopkeepers = new ObservableCollection<ShopkeeperItem>();
        ZoneTransfers = new ObservableCollection<ZoneTransferItem>();
        ZoneVisualizationObjects = new ObservableCollection<ZoneVisualizationObject>();
        CollisionVisualizationObjects = new ObservableCollection<CollisionVisualizationObject>();
        NifMeshVisualizationObjects = new ObservableCollection<NifMeshVisualizationObject>();
        SpawnVisualizationObjects = new ObservableCollection<SpawnVisualizationObject>();
        PathVisualizationObjects = new ObservableCollection<PathVisualizationObject>();
        NodeVisualizationObjects = new ObservableCollection<NodeVisualizationObject>();
        
        _nifProcessor = new NifGeometryProcessor();
        
        LoadAvailableZones();
    }

    public ObservableCollection<string> Zones { get; }
    public ObservableCollection<ZoneObjectItem> ZoneObjects { get; }
    public ObservableCollection<ShopkeeperItem> Shopkeepers { get; }
    public ObservableCollection<ZoneTransferItem> ZoneTransfers { get; }
    public ObservableCollection<ZoneVisualizationObject> ZoneVisualizationObjects { get; }
    public ObservableCollection<CollisionVisualizationObject> CollisionVisualizationObjects { get; }
    public ObservableCollection<NifMeshVisualizationObject> NifMeshVisualizationObjects { get; }
    public ObservableCollection<SpawnVisualizationObject> SpawnVisualizationObjects { get; }
    public ObservableCollection<PathVisualizationObject> PathVisualizationObjects { get; }
    public ObservableCollection<NodeVisualizationObject> NodeVisualizationObjects { get; }

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

    public bool ShowCollisions
    {
        get => _showCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showCollisions, value);
            UpdateVisibility();
        }
    }

    public bool ShowZoneObjects
    {
        get => _showZoneObjects;
        set
        {
            this.RaiseAndSetIfChanged(ref _showZoneObjects, value);
            UpdateVisibility();
        }
    }

    public bool ShowNifGeometry
    {
        get => _showNifGeometry;
        set
        {
            this.RaiseAndSetIfChanged(ref _showNifGeometry, value);
            UpdateVisibility();
        }
    }

    public bool ShowSpawns
    {
        get => _showSpawns;
        set
        {
            this.RaiseAndSetIfChanged(ref _showSpawns, value);
            UpdateVisibility();
        }
    }

    public bool ShowPaths
    {
        get => _showPaths;
        set
        {
            this.RaiseAndSetIfChanged(ref _showPaths, value);
            UpdateVisibility();
        }
    }

    public bool ShowNodes
    {
        get => _showNodes;
        set
        {
            this.RaiseAndSetIfChanged(ref _showNodes, value);
            UpdateVisibility();
        }
    }

    // Collision shape filter properties
    public bool ShowBoxCollisions
    {
        get => _showBoxCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showBoxCollisions, value);
            UpdateVisibility();
        }
    }

    public bool ShowSphereCollisions
    {
        get => _showSphereCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showSphereCollisions, value);
            UpdateVisibility();
        }
    }

    public bool ShowCylinderCollisions
    {
        get => _showCylinderCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showCylinderCollisions, value);
            UpdateVisibility();
        }
    }

    public bool ShowTubeCollisions
    {
        get => _showTubeCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showTubeCollisions, value);
            UpdateVisibility();
        }
    }

    public bool ShowPlaneCollisions
    {
        get => _showPlaneCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showPlaneCollisions, value);
            UpdateVisibility();
        }
    }

    public bool ShowMeshCollisions
    {
        get => _showMeshCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showMeshCollisions, value);
            UpdateVisibility();
        }
    }

    public bool ShowRayCollisions
    {
        get => _showRayCollisions;
        set
        {
            this.RaiseAndSetIfChanged(ref _showRayCollisions, value);
            UpdateVisibility();
        }
    }

    public bool HasSelectedObject => SelectedObject != null;

    // Computed properties for filter counts
    public int VisibleCollisionCount => CollisionVisualizationObjects.Count(ShouldShowCollision);
    public int BoxCollisionCount => CollisionVisualizationObjects.Count(c => c.GeometryType == "Box");
    public int SphereCollisionCount => CollisionVisualizationObjects.Count(c => c.GeometryType == "Sphere");
    public int CylinderCollisionCount => CollisionVisualizationObjects.Count(c => c.GeometryType == "Cylinder");
    public int TubeCollisionCount => CollisionVisualizationObjects.Count(c => c.GeometryType == "Tube");
    public int PlaneCollisionCount => CollisionVisualizationObjects.Count(c => c.GeometryType == "Plane");
    public int MeshCollisionCount => CollisionVisualizationObjects.Count(c => c.GeometryType == "Mesh");
    public int RayCollisionCount => CollisionVisualizationObjects.Count(c => c.GeometryType == "Ray");

    // Viewport properties
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            value = Math.Max(MinZoom, Math.Min(MaxZoom, value));
            this.RaiseAndSetIfChanged(ref _zoomLevel, value);
            UpdateCanvasTransform();
        }
    }

    public double PanX
    {
        get => _panX;
        set
        {
            this.RaiseAndSetIfChanged(ref _panX, value);
            UpdateCanvasTransform();
        }
    }

    public double PanY
    {
        get => _panY;
        set
        {
            this.RaiseAndSetIfChanged(ref _panY, value);
            UpdateCanvasTransform();
        }
    }

    public string ZoomDisplayText => $"Zoom: {ZoomLevel:P0}";

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
    public ICommand SelectZoneCommand { get; }
    
    // Viewport commands
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetViewCommand { get; }
    public ICommand PanUpCommand { get; }
    public ICommand PanDownCommand { get; }
    public ICommand PanLeftCommand { get; }
    public ICommand PanRightCommand { get; }
    
    public void SetZoneObjectCanvas(Canvas canvas)
    {
        _zoneObjectCanvas = canvas;
    }

    private void SelectZone()
    {
        LoadZone();
    }

    private async void LoadZone()
    {
        try
        {
            // Create and show zone selection dialog
            var dialogViewModel = new ZoneSelectionDialogViewModel(Zones);
            var selectedZone = await ShowZoneSelectionDialog(dialogViewModel);
            
            if (string.IsNullOrEmpty(selectedZone))
            {
                return; // User cancelled
            }

            IsLoading = true;
            SelectedZone = selectedZone;
            Console.WriteLine($"Loading zone: {SelectedZone}");
            
            MessageService.Info($"Loading zone: {SelectedZone}")
                .WithDuration(TimeSpan.FromSeconds(2))
                .Send();

            var (zoneData, collisionData, sceneFile, spawnData, pathData, nodeData) = await _zoneDataService.LoadZoneDataAsync(SelectedZone);
            _currentZoneData = zoneData;
            _currentCollisionData = collisionData;
            _currentSceneFile = sceneFile;
            _currentSpawnData = spawnData;
            _currentPathData = pathData;
            _currentNodeData = nodeData;
            
            if (_currentZoneData != null)
            {
                await PopulateZoneData(_currentZoneData);
                
                if (_currentCollisionData != null)
                {
                    await PopulateCollisionData(_currentCollisionData);
                }
                
                if (_currentSceneFile != null)
                {
                    await PopulateNifGeometry(_currentSceneFile);
                }
                
                // Process path data if available
                if (_currentSpawnData != null || _currentPathData != null || _currentNodeData != null)
                {
                    await PopulatePathData(_currentSpawnData, _currentPathData, _currentNodeData);
                }
                
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
            Console.WriteLine($"Error loading zone: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async System.Threading.Tasks.Task<string?> ShowZoneSelectionDialog(ZoneSelectionDialogViewModel dialogViewModel)
    {
        // Get the main window from the application lifetime
        var appLifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (appLifetime?.MainWindow != null)
        {
            return await Views.ZoneSelectionDialog.ShowDialogAsync(appLifetime.MainWindow, dialogViewModel);
        }
        
        // Fallback - this shouldn't happen in normal operation
        MessageService.Error("Cannot show zone selection dialog - main window not available")
            .WithDuration(TimeSpan.FromSeconds(3))
            .Send();
        return null;
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
        CollisionVisualizationObjects.Clear();
        NifMeshVisualizationObjects.Clear();
        SpawnVisualizationObjects.Clear();
        PathVisualizationObjects.Clear();
        NodeVisualizationObjects.Clear();
        
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

    private async Task PopulateCollisionData(Bcd collisionData)
    {
        var tempCollisionObjects = new List<CollisionVisualizationObject>();
        
        await Task.Run(() =>
        {
            Console.WriteLine($"PopulateCollisionData called with {collisionData.Collisions.Count} collision objects");
            
            foreach (var collision in collisionData.Collisions)
            {
                var collisionObj = new CollisionVisualizationObject
                {
                    Name = !string.IsNullOrEmpty(collision.Geometry.Name) ? collision.Geometry.Name : $"Collision_{collision.Geometry.Params.TypeId}",
                    GeometryType = GetGeometryTypeName(collision.Geometry.Params.TypeId),
                    X = collision.Geometry.Location[0],
                    Y = collision.Geometry.Location[1],
                    Z = collision.Geometry.Location[2],
                    Scale = collision.Geometry.Scale,
                    Material = collision.Geometry.Material,
                    CategoryFlags = collision.CategoryFlags,
                    CollisionFlags = collision.CollisionFlags,
                    GeometryParams = collision.Geometry.Params,
                    Mesh = collision.Mesh
                };
                
                // Copy rotation matrix
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        collisionObj.Rotation[i, j] = collision.Geometry.Rotation[i, j];
                    }
                }
                
                tempCollisionObjects.Add(collisionObj);
                
                // Debug: Log first few collision objects
                if (tempCollisionObjects.Count <= 5)
                {
                    Console.WriteLine($"Collision: {collisionObj.Name} at ({collisionObj.X:F1}, {collisionObj.Y:F1}, {collisionObj.Z:F1}) - Type: {collisionObj.GeometryType}");
                }
            }
            
            Console.WriteLine($"Collision processing complete. Created {tempCollisionObjects.Count} collision visualization objects");
        });
        
        // Update UI collection on UI thread
        foreach (var item in tempCollisionObjects)
            CollisionVisualizationObjects.Add(item);
            
        Console.WriteLine($"CollisionVisualizationObjects updated. Final count: {CollisionVisualizationObjects.Count}");
        
        // Notify property changes for collision counts
        this.RaisePropertyChanged(nameof(VisibleCollisionCount));
        this.RaisePropertyChanged(nameof(BoxCollisionCount));
        this.RaisePropertyChanged(nameof(SphereCollisionCount));
        this.RaisePropertyChanged(nameof(CylinderCollisionCount));
        this.RaisePropertyChanged(nameof(TubeCollisionCount));
        this.RaisePropertyChanged(nameof(PlaneCollisionCount));
        this.RaisePropertyChanged(nameof(MeshCollisionCount));
        this.RaisePropertyChanged(nameof(RayCollisionCount));
        
        // Create collision visuals now that we have collision data
        CreateCollisionVisuals();
    }

    private async Task PopulateNifGeometry(NifFile sceneFile)
    {
        var tempNifMeshObjects = new List<NifMeshVisualizationObject>();
        
        await Task.Run(() =>
        {
            Console.WriteLine($"PopulateNifGeometry called with NIF file containing {sceneFile.Blocks.Length} blocks");
            
            if (_nifProcessor != null)
            {
                var processedGeometry = _nifProcessor.ProcessNifFile(sceneFile);
                
                Console.WriteLine($"Processed NIF geometry: {processedGeometry.Meshes.Count} meshes, has geometry: {processedGeometry.HasGeometry}");
                
                foreach (var mesh in processedGeometry.Meshes)
                {
                    if (mesh.HasValidGeometry)
                    {
                        var nifMeshObj = new NifMeshVisualizationObject
                        {
                            Name = mesh.Name,
                            Vertices2D = mesh.Vertices2D,
                            Triangles = mesh.Triangles,
                            BoundingBox = mesh.BoundingBox,
                            VertexCount = mesh.Vertices2D.Count,
                            TriangleCount = mesh.Triangles.Count
                        };
                        
                        tempNifMeshObjects.Add(nifMeshObj);
                        
                        // Debug: Log first few mesh objects
                        if (tempNifMeshObjects.Count <= 5)
                        {
                            Console.WriteLine($"NIF Mesh: {nifMeshObj.Name} with {nifMeshObj.VertexCount} vertices and {nifMeshObj.TriangleCount} triangles");
                        }
                    }
                }
            }
            
            Console.WriteLine($"NIF geometry processing complete. Created {tempNifMeshObjects.Count} mesh visualization objects");
        });
        
        // Update UI collection on UI thread
        foreach (var item in tempNifMeshObjects)
            NifMeshVisualizationObjects.Add(item);
            
        Console.WriteLine($"NifMeshVisualizationObjects updated. Final count: {NifMeshVisualizationObjects.Count}");
        
        // Create NIF geometry visuals now that we have mesh data
        CreateNifGeometryVisuals();
    }

    private async Task PopulatePathData(SpawnManager? spawnData, PathTemplateList? pathData, NodeTemplateList? nodeData)
    {
        var tempSpawns = new List<SpawnVisualizationObject>();
        var tempPaths = new List<PathVisualizationObject>();
        var tempNodes = new List<NodeVisualizationObject>();
        
        await Task.Run(() =>
        {
            Console.WriteLine($"PopulatePathData called with:");
            Console.WriteLine($"  - Spawn data: {(spawnData != null ? $"{spawnData.m_spawners?.Count ?? 0} spawners" : "null")}");
            Console.WriteLine($"  - Path data: {(pathData != null ? $"{pathData.m_pathList?.Count ?? 0} paths" : "null")}");
            Console.WriteLine($"  - Node data: {(nodeData != null ? $"{nodeData.m_nodeList?.Count ?? 0} nodes" : "null")}");
            
            // Process nodes first since paths reference them
            Dictionary<ulong, NodeVisualizationObject> nodeMap = new();
            if (nodeData?.m_nodeList != null)
            {
                Console.WriteLine($"Node data contains {nodeData.m_nodeList.Count} node entries");
                
                foreach (var nodeObj in nodeData.m_nodeList)
                {
                    if (nodeObj != null)
                    {
                        Console.WriteLine($"Processing node: ID={nodeObj.m_id}, Location=({nodeObj.m_location.X}, {nodeObj.m_location.Y}, {nodeObj.m_location.Z})");
                        
                        var nodeVis = new NodeVisualizationObject
                        {
                            NodeId = (ulong)nodeObj.m_id,
                            Name = $"Node_{nodeObj.m_id}", // NodeObject doesn't have m_zoneTag
                            X = nodeObj.m_location.X,
                            Y = nodeObj.m_location.Y,
                            Z = nodeObj.m_location.Z
                        };
                        
                        tempNodes.Add(nodeVis);
                        nodeMap[nodeVis.NodeId] = nodeVis;
                    }
                    else
                    {
                        Console.WriteLine("Found null node object in node list");
                    }
                }
                Console.WriteLine($"Processed {tempNodes.Count} nodes from {nodeData.m_nodeList.Count} entries");
            }
            else
            {
                Console.WriteLine("Node data is null or m_nodeList is null");
            }
            
            // Process paths and connect them to nodes
            Dictionary<ulong, PathVisualizationObject> pathMap = new();
            if (pathData?.m_pathList != null)
            {
                foreach (var pathTemplate in pathData.m_pathList)
                {
                    if (pathTemplate != null)
                    {
                        var pathVis = new PathVisualizationObject
                        {
                            PathId = (ulong)pathTemplate.m_id,
                            Name = pathTemplate.m_name ?? $"Path_{pathTemplate.m_id}",
                            PathType = "Mob Path"
                        };
                        
                        // Connect nodes to this path
                        if (pathTemplate.m_nodeIDs != null)
                        {
                            var pathNodes = new List<NodeVisualizationObject>();
                            for (int i = 0; i < pathTemplate.m_nodeIDs.Count; i++)
                            {
                                var nodeId = (ulong)pathTemplate.m_nodeIDs[i];
                                if (nodeMap.TryGetValue(nodeId, out var node))
                                {
                                    node.PathId = pathVis.PathId;
                                    node.NodeIndex = i;
                                    pathNodes.Add(node);
                                }
                            }
                            pathVis.Nodes = pathNodes;
                        }
                        
                        tempPaths.Add(pathVis);
                        pathMap[pathVis.PathId] = pathVis;
                    }
                }
                Console.WriteLine($"Processed {tempPaths.Count} paths");
            }
            
            // Skip spawn processing since spawn data doesn't contain location info
            // Spawns are just metadata about what creatures use paths - the physical locations are the nodes
            
            Console.WriteLine($"Path data processing complete. Spawns: {tempSpawns.Count}, Paths: {tempPaths.Count}, Nodes: {tempNodes.Count}");
        });
        
        // Update UI collections on UI thread
        foreach (var item in tempSpawns)
            SpawnVisualizationObjects.Add(item);
            
        foreach (var item in tempPaths)
            PathVisualizationObjects.Add(item);
            
        foreach (var item in tempNodes)
            NodeVisualizationObjects.Add(item);
            
        Console.WriteLine($"Path UI collections updated. Final counts: Spawns: {SpawnVisualizationObjects.Count}, Paths: {PathVisualizationObjects.Count}, Nodes: {NodeVisualizationObjects.Count}");
        
        // Create path visuals now that we have path data
        CreatePathVisuals();
    }

    private string GetGeometryTypeName(uint typeId)
    {
        return typeId switch
        {
            0 => "Box",
            1 => "Ray", 
            2 => "Sphere",
            3 => "Cylinder",
            4 => "Tube",
            5 => "Plane",
            6 => "Mesh",
            _ => $"Unknown({typeId})"
        };
    }

    // Viewport control methods
    private void ZoomIn()
    {
        ZoomLevel += ZoomStep;
    }

    private void ZoomOut()
    {
        ZoomLevel -= ZoomStep;
    }

    private void ResetView()
    {
        ZoomLevel = 1.0;
        PanX = 0.0;
        PanY = 0.0;
    }

    private void PanUp()
    {
        PanY -= PanStep / ZoomLevel;
    }

    private void PanDown()
    {
        PanY += PanStep / ZoomLevel;
    }

    private void PanLeft()
    {
        PanX -= PanStep / ZoomLevel;
    }

    private void PanRight()
    {
        PanX += PanStep / ZoomLevel;
    }

    public void HandleMouseWheel(double delta, Avalonia.Point mousePosition)
    {
        var oldZoom = ZoomLevel;
        var zoomFactor = delta > 0 ? 1 + ZoomStep : 1 - ZoomStep;
        var newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, oldZoom * zoomFactor));
        
        if (Math.Abs(newZoom - oldZoom) > 0.001)
        {
            // Zoom towards mouse position
            var zoomRatio = newZoom / oldZoom;
            PanX = mousePosition.X - (mousePosition.X - PanX) * zoomRatio;
            PanY = mousePosition.Y - (mousePosition.Y - PanY) * zoomRatio;
            ZoomLevel = newZoom;
        }
    }

    public void HandleMouseDrag(double deltaX, double deltaY)
    {
        PanX += deltaX;
        PanY += deltaY;
    }

    private void UpdateCanvasTransform()
    {
        if (_zoneObjectCanvas == null)
            return;

        var transform = new TransformGroup();
        transform.Children.Add(new TranslateTransform(PanX, PanY));
        transform.Children.Add(new ScaleTransform(ZoomLevel, ZoomLevel));
        
        _zoneObjectCanvas.RenderTransform = transform;
        
        // Update zoom display
        this.RaisePropertyChanged(nameof(ZoomDisplayText));
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
                
                // Tag for identification
                border.Tag = obj;
                
                // Set initial visibility
                border.IsVisible = ShowZoneObjects;
                
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
        
        // Create collision visuals if available and enabled
        CreateCollisionVisuals();
    }

    private bool ShouldShowCollision(CollisionVisualizationObject collision)
    {
        if (!ShowCollisions)
            return false;

        // Check shape-based filters
        var geometryType = collision.GeometryType;
        
        return geometryType switch
        {
            "Box" => ShowBoxCollisions,
            "Sphere" => ShowSphereCollisions,
            "Cylinder" => ShowCylinderCollisions,
            "Tube" => ShowTubeCollisions,
            "Plane" => ShowPlaneCollisions,
            "Mesh" => ShowMeshCollisions,
            "Ray" => ShowRayCollisions,
            _ => true // Show unknown geometry types by default
        };
    }

    private void UpdateVisibility()
    {
        if (_zoneObjectCanvas == null)
            return;
            
        // Update zone object visibility
        var zoneObjectVisuals = _zoneObjectCanvas.Children
            .OfType<Control>()
            .Where(c => c.Tag is ZoneVisualizationObject)
            .ToList();
            
        foreach (var visual in zoneObjectVisuals)
        {
            visual.IsVisible = ShowZoneObjects;
        }
        
        // Update collision object visibility
        var collisionVisuals = _zoneObjectCanvas.Children
            .OfType<Control>()
            .Where(c => c.Tag is CollisionVisualizationObject)
            .ToList();
            
        foreach (var visual in collisionVisuals)
        {
            if (visual.Tag is CollisionVisualizationObject collision)
            {
                visual.IsVisible = ShouldShowCollision(collision);
            }
        }
        
        // Update NIF geometry visibility
        var nifGeometryVisuals = _zoneObjectCanvas.Children
            .OfType<Control>()
            .Where(c => c.Tag is NifMeshVisualizationObject)
            .ToList();
            
        foreach (var visual in nifGeometryVisuals)
        {
            visual.IsVisible = ShowNifGeometry;
        }
        
        // Update spawn visibility
        var spawnVisuals = _zoneObjectCanvas.Children
            .OfType<Control>()
            .Where(c => c.Tag is SpawnVisualizationObject)
            .ToList();
            
        foreach (var visual in spawnVisuals)
        {
            visual.IsVisible = ShowSpawns;
        }
        
        // Update path visibility (path lines)
        var pathVisuals = _zoneObjectCanvas.Children
            .OfType<Control>()
            .Where(c => c.Tag is PathVisualizationObject)
            .ToList();
            
        foreach (var visual in pathVisuals)
        {
            visual.IsVisible = ShowPaths;
        }
        
        // Update node visibility
        var nodeVisuals = _zoneObjectCanvas.Children
            .OfType<Control>()
            .Where(c => c.Tag is NodeVisualizationObject)
            .ToList();
            
        foreach (var visual in nodeVisuals)
        {
            visual.IsVisible = ShowNodes;
        }
        
        // Notify property changes for count updates
        this.RaisePropertyChanged(nameof(VisibleCollisionCount));
    }

    private void CreateCollisionVisuals()
    {
        if (_zoneObjectCanvas == null)
        {
            Console.WriteLine("Canvas not set, cannot create collision visuals");
            return;
        }
        
        foreach (var collision in CollisionVisualizationObjects)
        {
            try
            {
                var shouldShow = ShouldShowCollision(collision);
                var visual = CreateCollisionShapeVisual(collision);
                if (visual != null)
                {
                    // Set initial visibility based on filters
                    visual.IsVisible = shouldShow;
                    _zoneObjectCanvas.Children.Add(visual);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating collision visual for '{collision.Name}': {ex.Message}");
            }
        }
        
    }

    private void CreateNifGeometryVisuals()
    {
        if (_zoneObjectCanvas == null)
        {
            Console.WriteLine("Canvas not set, cannot create NIF geometry visuals");
            return;
        }
        
        Console.WriteLine($"Creating NIF geometry visuals for {NifMeshVisualizationObjects.Count} mesh objects");
        
        foreach (var mesh in NifMeshVisualizationObjects)
        {
            try
            {
                var visual = CreateNifMeshVisual(mesh);
                if (visual != null)
                {
                    // Set initial visibility based on ShowNifGeometry setting
                    visual.IsVisible = ShowNifGeometry;
                    _zoneObjectCanvas.Children.Add(visual);
                    Console.WriteLine($"Added NIF mesh visual for '{mesh.Name}' to canvas (visible: {visual.IsVisible})");
                }
                else
                {
                    Console.WriteLine($"Failed to create visual for NIF mesh '{mesh.Name}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating NIF mesh visual for '{mesh.Name}': {ex.Message}");
            }
        }
        
        Console.WriteLine($"Created NIF geometry visuals on canvas");
    }
    
    private void CreatePathVisuals()
    {
        if (_zoneObjectCanvas == null)
        {
            Console.WriteLine("Canvas not set, cannot create path visuals");
            return;
        }
        
        Console.WriteLine($"Creating path visuals for {SpawnVisualizationObjects.Count} spawns, {PathVisualizationObjects.Count} paths, {NodeVisualizationObjects.Count} nodes");
        Console.WriteLine($"Canvas is null: {_zoneObjectCanvas == null}, ShowSpawns: {ShowSpawns}, ShowPaths: {ShowPaths}, ShowNodes: {ShowNodes}");
        
        // Create spawn visuals
        foreach (var spawn in SpawnVisualizationObjects)
        {
            try
            {
                var visual = CreateSpawnVisual(spawn);
                if (visual != null)
                {
                    visual.IsVisible = ShowSpawns;
                    _zoneObjectCanvas.Children.Add(visual);
                    Console.WriteLine($"Added spawn visual for '{spawn.Name}' to canvas (visible: {visual.IsVisible})");
                }
                else
                {
                    Console.WriteLine($"Failed to create visual for spawn '{spawn.Name}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating spawn visual for '{spawn.Name}': {ex.Message}");
            }
        }
        
        // Create node visuals
        foreach (var node in NodeVisualizationObjects)
        {
            try
            {
                var visual = CreateNodeVisual(node);
                if (visual != null)
                {
                    visual.IsVisible = ShowNodes;
                    _zoneObjectCanvas.Children.Add(visual);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating node visual for '{node.Name}': {ex.Message}");
            }
        }
        
        // Create path visuals (lines connecting nodes)
        foreach (var path in PathVisualizationObjects)
        {
            try
            {
                if (path.HasNodes)
                {
                    var visuals = CreatePathLineVisuals(path);
                    foreach (var visual in visuals)
                    {
                        visual.IsVisible = ShowPaths;
                        _zoneObjectCanvas.Children.Add(visual);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating path visuals for '{path.Name}': {ex.Message}");
            }
        }
        
        Console.WriteLine($"Created path visuals on canvas. Total children in canvas: {_zoneObjectCanvas?.Children.Count ?? 0}");
    }
    
    private Control? CreateSpawnVisual(SpawnVisualizationObject spawn)
    {
        // Convert coordinates (same as zone objects)
        var canvasX = 10000.0 + (spawn.X * 0.25);
        var canvasY = 10000.0 - (spawn.Y * 0.25);
        
        Console.WriteLine($"Creating spawn visual for '{spawn.Name}' at game coords ({spawn.X:F1}, {spawn.Y:F1}, {spawn.Z:F1}) -> canvas coords ({canvasX:F1}, {canvasY:F1})");
        
        // Use a distinctive color for spawns - bright orange
        var spawnBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 140, 0)); // Orange
        
        var spawn_visual = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = spawnBrush,
            Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 255)), // White outline
            StrokeThickness = 2
        };
        
        // Position on canvas
        Canvas.SetLeft(spawn_visual, canvasX - 6); // Center the circle
        Canvas.SetTop(spawn_visual, canvasY - 6);
        
        // Add tooltip
        ToolTip.SetTip(spawn_visual, $"{spawn.Name} ({spawn.SpawnType})\nID: {spawn.SpawnId}\nTemplate: {spawn.TemplateId}\nAt: ({spawn.X:F1}, {spawn.Y:F1}, {spawn.Z:F1})\n{spawn.PathInfo}");
        
        // Tag for identification
        spawn_visual.Tag = spawn;
        
        return spawn_visual;
    }
    
    private Control? CreateNodeVisual(NodeVisualizationObject node)
    {
        // Convert coordinates (same as zone objects)
        var canvasX = 10000.0 + (node.X * 0.25);
        var canvasY = 10000.0 - (node.Y * 0.25);
        
        // Use a distinctive color for nodes - bright green
        var nodeBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 255, 0)); // Bright green
        
        var node_visual = new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = nodeBrush,
            Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 255)), // White outline
            StrokeThickness = 1
        };
        
        // Position on canvas
        Canvas.SetLeft(node_visual, canvasX - 3); // Center the circle
        Canvas.SetTop(node_visual, canvasY - 3);
        
        // Add tooltip
        ToolTip.SetTip(node_visual, $"{node.DisplayText}\nID: {node.NodeId}\nPath: {node.PathId}\nAt: ({node.X:F1}, {node.Y:F1}, {node.Z:F1})");
        
        // Tag for identification
        node_visual.Tag = node;
        
        return node_visual;
    }
    
    private List<Control> CreatePathLineVisuals(PathVisualizationObject path)
    {
        var visuals = new List<Control>();
        
        if (path.Nodes.Count < 2) return visuals;
        
        // Create lines connecting consecutive nodes
        for (int i = 0; i < path.Nodes.Count - 1; i++)
        {
            var fromNode = path.Nodes[i];
            var toNode = path.Nodes[i + 1];
            
            // Convert coordinates
            var fromX = 10000.0 + (fromNode.X * 0.25);
            var fromY = 10000.0 - (fromNode.Y * 0.25);
            var toX = 10000.0 + (toNode.X * 0.25);
            var toY = 10000.0 - (toNode.Y * 0.25);
            
            // Create line
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(fromX, fromY),
                EndPoint = new Avalonia.Point(toX, toY),
                Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 0)), // Yellow path lines
                StrokeThickness = 2,
                Opacity = 0.8
            };
            
            // Add tooltip
            ToolTip.SetTip(line, $"{path.Name}\nSegment {i + 1} of {path.Nodes.Count - 1}\nFrom: {fromNode.DisplayText}\nTo: {toNode.DisplayText}");
            
            // Tag for identification
            line.Tag = path;
            
            visuals.Add(line);
        }
        
        return visuals;
    }

    private Control? CreateNifMeshVisual(NifMeshVisualizationObject mesh)
    {
        if (mesh.Vertices2D.Count == 0)
            return null;

        try
        {
            // For large meshes, use simplified representation
            var vertices = mesh.Vertices2D.Count > 100 
                ? NifGeometryProcessor.SimplifyMesh(mesh.Vertices2D, 100)
                : mesh.Vertices2D;

            // If we still have too many vertices, create a bounding box representation
            if (vertices.Count > 50)
            {
                return CreateNifBoundingBoxVisual(mesh);
            }

            // Create a polygon for the mesh
            var polygon = new Polygon
            {
                Points = vertices,
                Fill = new SolidColorBrush(Avalonia.Media.Color.FromArgb(100, 255, 255, 0)), // Semi-transparent yellow
                Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 0)), // Yellow outline
                StrokeThickness = 1,
                Opacity = 0.7
            };

            // Add tooltip
            ToolTip.SetTip(polygon, $"NIF Mesh: {mesh.Name}\nVertices: {mesh.VertexCount}\nTriangles: {mesh.TriangleCount}");
            
            // Tag for identification
            polygon.Tag = mesh;

            return polygon;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating polygon for mesh '{mesh.Name}': {ex.Message}");
            return CreateNifBoundingBoxVisual(mesh);
        }
    }

    private Control CreateNifBoundingBoxVisual(NifMeshVisualizationObject mesh)
    {
        // Create a simple rectangle representing the mesh bounds
        if (mesh.BoundingBox.Count >= 4)
        {
            var minX = mesh.BoundingBox.Min(p => p.X);
            var maxX = mesh.BoundingBox.Max(p => p.X);
            var minY = mesh.BoundingBox.Min(p => p.Y);
            var maxY = mesh.BoundingBox.Max(p => p.Y);

            var rectangle = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = Math.Max(2, maxX - minX),
                Height = Math.Max(2, maxY - minY),
                Fill = new SolidColorBrush(Avalonia.Media.Color.FromArgb(80, 255, 255, 0)), // Semi-transparent yellow
                Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 0)), // Yellow outline
                StrokeThickness = 1,
                Opacity = 0.6
            };

            // Position the rectangle
            Canvas.SetLeft(rectangle, minX);
            Canvas.SetTop(rectangle, minY);

            // Add tooltip
            ToolTip.SetTip(rectangle, $"NIF Mesh (Simplified): {mesh.Name}\nVertices: {mesh.VertexCount}\nTriangles: {mesh.TriangleCount}");
            
            // Tag for identification
            rectangle.Tag = mesh;

            return rectangle;
        }

        // Fallback: create a small marker
        var marker = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 0)),
            Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 200, 0)),
            StrokeThickness = 1
        };

        // Center at canvas origin
        Canvas.SetLeft(marker, CanvasCenterX - 4);
        Canvas.SetTop(marker, CanvasCenterY - 4);

        // Add tooltip
        ToolTip.SetTip(marker, $"NIF Mesh (Marker): {mesh.Name}\nVertices: {mesh.VertexCount}");
        
        // Tag for identification
        marker.Tag = mesh;

        return marker;
    }


    private Control? CreateCollisionShapeVisual(CollisionVisualizationObject collision)
    {
        // Convert coordinates (same as zone objects)
        var canvasX = 10000.0 + (collision.X * 0.25);
        var canvasY = 10000.0 - (collision.Y * 0.25);
        
        // Neon blue color as requested
        var collisionBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 255, 255)); // #00FFFF
        var collisionStroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 200, 255)); // Slightly darker blue for stroke
        
        Control? visual = collision.GeometryParams switch
        {
            BcdGeomParams.BoxGeomParams box => CreateBoxVisual(box, collision.Scale),
            BcdGeomParams.SphereGeomParams sphere => CreateSphereVisual(sphere, collision.Scale),
            BcdGeomParams.CylinderGeomParams cylinder => CreateCylinderVisual(cylinder, collision.Scale),
            BcdGeomParams.TubeGeomParams tube => CreateTubeVisual(tube, collision.Scale),
            BcdGeomParams.PlaneGeomParams plane => CreatePlaneVisual(plane, collision.Scale),
            BcdGeomParams.MeshGeomParams => CreateMeshVisual(collision.Mesh, collision.Scale),
            BcdGeomParams.RayGeomParams ray => CreateRayVisual(ray, collision.Scale),
            _ => null
        };
        
        if (visual != null)
        {
            // Apply neon blue styling
            if (visual is Shape shape)
            {
                shape.Fill = collisionBrush;
                shape.Stroke = collisionStroke;
                shape.StrokeThickness = 1;
                shape.Opacity = 0.6; // Semi-transparent
            }
            
            // Position on canvas
            Canvas.SetLeft(visual, canvasX);
            Canvas.SetTop(visual, canvasY);
            
            // Apply rotation if needed (simplified 2D rotation)
            ApplyRotationTransform(visual, collision.Rotation);
            
            // Add tooltip
            ToolTip.SetTip(visual, $"{collision.Name} ({collision.GeometryType})\nFlags: {collision.FlagsText}\nAt: ({collision.X:F1}, {collision.Y:F1}, {collision.Z:F1})");
            
            // Tag for identification
            visual.Tag = collision;
        }
        
        return visual;
    }

    private Avalonia.Controls.Shapes.Rectangle CreateBoxVisual(BcdGeomParams.BoxGeomParams box, float scale)
    {
        return new Avalonia.Controls.Shapes.Rectangle
        {
            Width = Math.Max(2, box.Length * scale * 0.25),
            Height = Math.Max(2, box.Width * scale * 0.25)
        };
    }

    private Ellipse CreateSphereVisual(BcdGeomParams.SphereGeomParams sphere, float scale)
    {
        var diameter = Math.Max(4, sphere.Radius * 2 * scale * 0.25);
        return new Ellipse
        {
            Width = diameter,
            Height = diameter
        };
    }

    private Ellipse CreateCylinderVisual(BcdGeomParams.CylinderGeomParams cylinder, float scale)
    {
        // For 2D view, cylinder appears as circle
        var diameter = Math.Max(4, cylinder.Radius * 2 * scale * 0.25);
        return new Ellipse
        {
            Width = diameter,
            Height = diameter
        };
    }

    private Ellipse CreateTubeVisual(BcdGeomParams.TubeGeomParams tube, float scale)
    {
        // For 2D view, tube appears as circle (similar to cylinder)
        var diameter = Math.Max(4, tube.Radius * 2 * scale * 0.25);
        return new Ellipse
        {
            Width = diameter,
            Height = diameter
        };
    }

    private Avalonia.Controls.Shapes.Line CreatePlaneVisual(BcdGeomParams.PlaneGeomParams plane, float scale)
    {
        // Create a line representing the plane intersection with the 2D view
        // Use normal vector to determine line orientation
        var length = 100 * scale * 0.25; // Fixed length for visualization
        
        return new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(-length/2, 0),
            EndPoint = new Avalonia.Point(length/2, 0),
            StrokeThickness = 2
        };
    }

    private Avalonia.Controls.Shapes.Line CreateRayVisual(BcdGeomParams.RayGeomParams ray, float scale)
    {
        var length = Math.Max(10, ray.Length * scale * 0.25);
        
        return new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(0, 0),
            EndPoint = new Avalonia.Point(length, 0),
            StrokeThickness = 2
        };
    }

    private Polygon? CreateMeshVisual(Imcodec.BCD.ProxyMesh? mesh, float scale)
    {
        if (mesh == null || mesh.Vertices.Count == 0)
            return null;
            
        // Create a simplified polygon from mesh vertices (project to 2D)
        var points = new List<Avalonia.Point>();
        
        foreach (var vertex in mesh.Vertices)
        {
            // Project 3D vertex to 2D (ignore Z for now)
            var x = vertex[0] * scale * 0.25;
            var y = -vertex[1] * scale * 0.25; // Flip Y for canvas
            points.Add(new Avalonia.Point(x, y));
        }
        
        // Limit to reasonable number of points for performance
        if (points.Count > 50)
        {
            points = points.Take(50).ToList();
        }
        
        return new Polygon
        {
            Points = points
        };
    }

    private void ApplyRotationTransform(Control visual, float[,] rotationMatrix)
    {
        // Apply simplified 2D rotation based on the 3D rotation matrix
        // Extract rotation around Z-axis for 2D representation
        var angle = Math.Atan2(rotationMatrix[1, 0], rotationMatrix[0, 0]) * 180.0 / Math.PI;
        
        if (Math.Abs(angle) > 0.1) // Only apply if there's significant rotation
        {
            visual.RenderTransform = new RotateTransform(angle);
        }
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
    
    private string DetermineSpawnType(SpawnObjectInfo spawn)
    {
        var name = spawn.m_zoneTag?.ToLower() ?? "";
        
        // Basic spawn type detection
        if (name.Contains("mob") || name.Contains("enemy") || name.Contains("creature"))
            return "Mob";
        if (name.Contains("npc") || name.Contains("character"))
            return "NPC";
        if (name.Contains("boss") || name.Contains("elite"))
            return "Boss";
        if (name.Contains("pet") || name.Contains("minion"))
            return "Pet";
            
        return "Unknown";
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

public class CollisionVisualizationObject
{
    public string Name { get; set; } = string.Empty;
    public string GeometryType { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Scale { get; set; } = 1.0f;
    public float[,] Rotation { get; set; } = new float[3, 3];
    public string Material { get; set; } = string.Empty;
    public CollisionFlags CategoryFlags { get; set; }
    public CollisionFlags CollisionFlags { get; set; }
    public BcdGeomParams.GeomParams GeometryParams { get; set; } = new BcdGeomParams.MeshGeomParams();
    public Imcodec.BCD.ProxyMesh? Mesh { get; set; }
    
    public string DisplayText => $"{Name} ({GeometryType})";
    public string CoordinateText => $"({X:F1}, {Y:F1}, {Z:F1})";
    public string FlagsText => $"Cat: {CategoryFlags}, Col: {CollisionFlags}";
}

public class NifMeshVisualizationObject
{
    public string Name { get; set; } = string.Empty;
    public List<Avalonia.Point> Vertices2D { get; set; } = new();
    public List<int[]> Triangles { get; set; } = new();
    public List<Avalonia.Point> BoundingBox { get; set; } = new();
    public int VertexCount { get; set; }
    public int TriangleCount { get; set; }
    
    public string DisplayText => $"{Name} (NIF Mesh)";
    public string GeometryInfo => $"Vertices: {VertexCount}, Triangles: {TriangleCount}";
    public bool HasValidGeometry => Vertices2D.Count > 0;
}

public class SpawnVisualizationObject
{
    public string Name { get; set; } = string.Empty;
    public ulong SpawnId { get; set; }
    public ulong TemplateId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Scale { get; set; } = 1.0f;
    public byte SpawnChance { get; set; } // Percentage chance to spawn (0-100)
    public string SpawnType { get; set; } = string.Empty;
    public List<ulong> PathIds { get; set; } = new(); // Paths this spawn uses
    
    public string DisplayText => $"{Name} (Spawn)";
    public string CoordinateText => $"({X:F1}, {Y:F1}, {Z:F1})";
    public string PathInfo => PathIds.Count > 0 ? $"Paths: {string.Join(", ", PathIds)}" : "No paths";
}

public class NodeVisualizationObject
{
    public ulong NodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public ulong PathId { get; set; } // Which path this node belongs to
    public int NodeIndex { get; set; } // Order in the path
    
    public string DisplayText => $"Node {NodeIndex} ({Name})";
    public string CoordinateText => $"({X:F1}, {Y:F1}, {Z:F1})";
}

public class PathVisualizationObject
{
    public ulong PathId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<NodeVisualizationObject> Nodes { get; set; } = new();
    public List<ulong> SpawnIds { get; set; } = new(); // Spawns that use this path
    public string PathType { get; set; } = "Unknown";
    
    public string DisplayText => $"{Name} (Path)";
    public string NodeInfo => $"Nodes: {Nodes.Count}";
    public string SpawnInfo => SpawnIds.Count > 0 ? $"Used by {SpawnIds.Count} spawn(s)" : "Unused path";
    public bool HasNodes => Nodes.Count > 0;
}
