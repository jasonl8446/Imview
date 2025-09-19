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
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;
using Avalonia.Controls.Shapes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Imview.Core.Services;
using Imview.Core.Models;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.ObjectProperty;
using Imcodec.Math;
using Imcodec.BCD;
using BcdGeomParams = Imcodec.BCD.GeomParams;
using WizardTea.Core;
using Imview.PacketReader.Services;

namespace Imview.Core.ViewModels;

public class ZoneEditorViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly ZoneDataService _zoneDataService;
    private string _selectedZone = string.Empty;
    private ZoneVisualizationObject? _selectedObject = null;
    private CoreObjectInfo? _selectedCoreObject = null;
    private PropertyClass? _selectedTemplate = null;
    private CollisionVisualizationObject? _selectedCollision = null;
    private PathVisualizationObject? _selectedPath = null;
    private NifMeshVisualizationObject? _selectedMesh = null;
    private VolumeVisualizationObject? _selectedVolume = null;
    private TriggerVisualizationObject? _selectedTrigger = null;
    private WizZoneData? _currentZoneData = null;
    private Bcd? _currentCollisionData = null;
    private NifFile? _currentSceneFile = null;
    private SpawnManager? _currentSpawnData = null;
    private PathTemplateList? _currentPathData = null;
    private NodeTemplateList? _currentNodeData = null;
    private WizZoneVolumes? _currentVolumeData = null;
    private WizZoneTriggers? _currentTriggerData = null;
    private NifGeometryProcessor? _nifProcessor = null;
    private bool _isLoading = false;
    private bool _showCollisions = true;
    private bool _showZoneObjects = true;
    private bool _showNifGeometry = true;
    private bool _showSpawns = true;
    private bool _showPaths = true;
    private bool _showNodes = true; // Make visible by default for debugging
    private bool _showVolumes = true; // Make visible by default
    private bool _showTriggers = true; // Make visible by default
    
    // Collision shape filters - default to true so all shapes show initially
    private bool _showBoxCollisions = true;
    private bool _showSphereCollisions = true;
    private bool _showCylinderCollisions = true;
    private bool _showTubeCollisions = true;
    private bool _showPlaneCollisions = true;
    private bool _showMeshCollisions = false;
    private bool _showRayCollisions = true;
    
    private Canvas? _zoneObjectCanvas = null;
    private ScrollViewer? _scrollViewer = null;
    
    // Viewport state for camera control
    private double _zoomLevel = 0.5; // Default to 50% zoom
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
        SelectCollisionCommand = ReactiveCommand.Create<CollisionVisualizationObject>(SelectCollision);
        SelectPathCommand = ReactiveCommand.Create<PathVisualizationObject>(SelectPath);
        SelectMeshCommand = ReactiveCommand.Create<NifMeshVisualizationObject>(SelectMesh);
        SelectVolumeCommand = ReactiveCommand.Create<VolumeVisualizationObject>(SelectVolume);
        SelectTriggerCommand = ReactiveCommand.Create<TriggerVisualizationObject>(SelectTrigger);
        SelectRelatedTriggerCommand = ReactiveCommand.Create<TriggerVisualizationObject>(SelectRelatedTrigger);
        SelectRelatedVolumeCommand = ReactiveCommand.Create<VolumeVisualizationObject>(SelectRelatedVolume);
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
        VolumeVisualizationObjects = new ObservableCollection<VolumeVisualizationObject>();
        TriggerVisualizationObjects = new ObservableCollection<TriggerVisualizationObject>();
        
        _nifProcessor = new NifGeometryProcessor();
        
        // Initialize viewport to show coordinates (0,0) in the center
        // Also ensure the initial zoom transform is applied
        InitializeViewportCenter();
        
        // Apply initial zoom transform (since field initialization doesn't trigger property setter)
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateCanvasTransform();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
        
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
    public ObservableCollection<VolumeVisualizationObject> VolumeVisualizationObjects { get; }
    public ObservableCollection<TriggerVisualizationObject> TriggerVisualizationObjects { get; }

    public string SelectedZone
    {
        get => _selectedZone;
        set {
            Console.WriteLine($"[DEBUG ZONE VM] SelectedZone changed to: '{value ?? "null"}'");
            this.RaiseAndSetIfChanged(ref _selectedZone, value);
        }
    }

    public ZoneVisualizationObject? SelectedObject
    {
        get => _selectedObject;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedObject, value);
            
            // Update SelectedCoreObject when a zone object is selected
            if (value != null && _currentZoneData?.m_objectList != null)
            {
                SelectedCoreObject = _currentZoneData.m_objectList
                    .FirstOrDefault(obj => obj != null && (ulong)obj.m_templateID == value.TemplateID);
            }
            else
            {
                SelectedCoreObject = null;
            }
            
            // Clear other selections when selecting a zone object
            if (value != null)
            {
                SelectedCollision = null;
                SelectedPath = null;
                SelectedMesh = null;
                SelectedVolume = null;
                SelectedTrigger = null;
            }
            
            // Notify property changes
            this.RaisePropertyChanged(nameof(HasSelectedObject));
            this.RaisePropertyChanged(nameof(HasSelectedAnyObject));
            NotifySelectionChanged();
            
            // Update visual selection in the viewport
            UpdateVisualSelection();
        }
    }

    public CoreObjectInfo? SelectedCoreObject
    {
        get => _selectedCoreObject;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCoreObject, value);
            
            // Load template data when a core object is selected
            if (value != null)
            {
                _ = Task.Run(async () =>
                {
                    var template = await LoadTemplateAsync(value);
                    SelectedTemplate = template;
                });
            }
            else
            {
                SelectedTemplate = null;
            }
        }
    }
    
    public PropertyClass? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
            this.RaisePropertyChanged(nameof(SelectedTemplateTypeName));
            this.RaisePropertyChanged(nameof(SelectedGameObjectTemplate));
            this.RaisePropertyChanged(nameof(SelectedGameObjectTemplateBehaviors));
            this.RaisePropertyChanged(nameof(SelectedTemplateDisplayNameResolved));
            this.RaisePropertyChanged(nameof(SelectedTemplateDisplayNameLocaleId));
            this.RaisePropertyChanged(nameof(SelectedTemplateDescriptionResolved));
            this.RaisePropertyChanged(nameof(SelectedTemplateDescriptionLocaleId));
            this.RaisePropertyChanged(nameof(SelectedTemplateObjectNameResolved));
            this.RaisePropertyChanged(nameof(SelectedTemplateObjectNameLocaleId));
        }
    }
    
    public string SelectedTemplateTypeName => SelectedTemplate?.GetType().Name ?? "Unknown";
    
    public GameObjectTemplate? SelectedGameObjectTemplate => SelectedTemplate as GameObjectTemplate;
    
    /// <summary>
    /// Gets the resolved English display name from the locale service for the selected template.
    /// Returns null if no template is selected or locale resolution fails.
    /// </summary>
    public string? SelectedTemplateDisplayNameResolved
    {
        get
        {
            var displayName = SelectedGameObjectTemplate?.m_displayName;
            if (string.IsNullOrWhiteSpace(displayName) || !LocaleService.Instance.IsLoaded)
            {
                return null;
            }
            
            return ResolveLocaleString(displayName);
        }
    }
    
    /// <summary>
    /// Gets the raw locale ID for the selected template's display name.
    /// </summary>
    public string? SelectedTemplateDisplayNameLocaleId
    {
        get
        {
            var displayName = SelectedGameObjectTemplate?.m_displayName;
            return string.IsNullOrWhiteSpace(displayName) ? null : displayName;
        }
    }
    
    /// <summary>
    /// Gets the resolved English description from the locale service for the selected template.
    /// Returns null if no template is selected or locale resolution fails.
    /// </summary>
    public string? SelectedTemplateDescriptionResolved
    {
        get
        {
            var description = SelectedGameObjectTemplate?.m_description;
            if (string.IsNullOrWhiteSpace(description) || !LocaleService.Instance.IsLoaded)
            {
                return null;
            }
            
            return ResolveLocaleString(description);
        }
    }
    
    /// <summary>
    /// Gets the raw locale ID for the selected template's description.
    /// </summary>
    public string? SelectedTemplateDescriptionLocaleId
    {
        get
        {
            var description = SelectedGameObjectTemplate?.m_description;
            return string.IsNullOrWhiteSpace(description) ? null : description;
        }
    }
    
    /// <summary>
    /// Gets the resolved English object name from the locale service for the selected template.
    /// Returns null if no template is selected or locale resolution fails.
    /// </summary>
    public string? SelectedTemplateObjectNameResolved
    {
        get
        {
            var objectName = SelectedGameObjectTemplate?.m_objectName;
            if (string.IsNullOrWhiteSpace(objectName) || !LocaleService.Instance.IsLoaded)
            {
                return null;
            }
            
            return ResolveLocaleString(objectName);
        }
    }
    
    /// <summary>
    /// Gets the raw locale ID for the selected template's object name.
    /// </summary>
    public string? SelectedTemplateObjectNameLocaleId
    {
        get
        {
            var objectName = SelectedGameObjectTemplate?.m_objectName;
            return string.IsNullOrWhiteSpace(objectName) ? null : objectName;
        }
    }
    
    public ObservableCollection<BehaviorWrapper> SelectedGameObjectTemplateBehaviors
    {
        get
        {
            var behaviors = new ObservableCollection<BehaviorWrapper>();
            if (SelectedGameObjectTemplate?.m_behaviors != null)
            {
                foreach (var behavior in SelectedGameObjectTemplate.m_behaviors)
                {
                    if (behavior != null)
                    {
                        behaviors.Add(new BehaviorWrapper(behavior));
                    }
                }
            }
            return behaviors;
        }
    }

    public CollisionVisualizationObject? SelectedCollision
    {
        get => _selectedCollision;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCollision, value);
            
            // Clear other selections when selecting a collision object
            if (value != null)
            {
                SelectedObject = null;
                SelectedCoreObject = null;
                SelectedPath = null;
                SelectedMesh = null;
                SelectedVolume = null;
                SelectedTrigger = null;
            }
            
            this.RaisePropertyChanged(nameof(HasSelectedCollision));
            this.RaisePropertyChanged(nameof(HasSelectedAnyObject));
            NotifySelectionChanged();
        }
    }

    public PathVisualizationObject? SelectedPath
    {
        get => _selectedPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPath, value);
            
            // Clear other selections when selecting a path object
            if (value != null)
            {
                SelectedObject = null;
                SelectedCoreObject = null;
                SelectedCollision = null;
                SelectedMesh = null;
                SelectedVolume = null;
                SelectedTrigger = null;
            }
            
            this.RaisePropertyChanged(nameof(HasSelectedPath));
            this.RaisePropertyChanged(nameof(HasSelectedAnyObject));
            NotifySelectionChanged();
        }
    }

    public NifMeshVisualizationObject? SelectedMesh
    {
        get => _selectedMesh;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMesh, value);
            
            // Clear other selections when selecting a mesh object
            if (value != null)
            {
                SelectedObject = null;
                SelectedCoreObject = null;
                SelectedCollision = null;
                SelectedPath = null;
                SelectedVolume = null;
                SelectedTrigger = null;
            }
            
            this.RaisePropertyChanged(nameof(HasSelectedMesh));
            this.RaisePropertyChanged(nameof(HasSelectedAnyObject));
            NotifySelectionChanged();
        }
    }

    public VolumeVisualizationObject? SelectedVolume
    {
        get => _selectedVolume;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedVolume, value);
            
            // Clear other selections when selecting a volume object
            if (value != null)
            {
                SelectedObject = null;
                SelectedCoreObject = null;
                SelectedCollision = null;
                SelectedPath = null;
                SelectedMesh = null;
                SelectedTrigger = null;
            }
            
            this.RaisePropertyChanged(nameof(HasSelectedVolume));
            this.RaisePropertyChanged(nameof(HasSelectedAnyObject));
            this.RaisePropertyChanged(nameof(SelectedVolumeEnterTriggers));
            this.RaisePropertyChanged(nameof(SelectedVolumeExitTriggers));
            NotifySelectionChanged();
        }
    }

    public TriggerVisualizationObject? SelectedTrigger
    {
        get => _selectedTrigger;
        set
        {
            Console.WriteLine($"[DEBUG ZONE VM] SelectedTrigger changed to: '{value?.Name ?? "null"}'");
            this.RaiseAndSetIfChanged(ref _selectedTrigger, value);
            
            // Clear other selections when selecting a trigger object
            if (value != null)
            {
                SelectedObject = null;
                SelectedCoreObject = null;
                SelectedCollision = null;
                SelectedPath = null;
                SelectedMesh = null;
                SelectedVolume = null;
            }
            
            this.RaisePropertyChanged(nameof(HasSelectedTrigger));
            this.RaisePropertyChanged(nameof(HasSelectedAnyObject));
            this.RaisePropertyChanged(nameof(SelectedTriggerActivatingVolumes));
            this.RaisePropertyChanged(nameof(SelectedTriggerDeactivatingVolumes));
            this.RaisePropertyChanged(nameof(SelectedTriggerFireVolumes));
            NotifySelectionChanged();
        }
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

    public bool ShowVolumes
    {
        get => _showVolumes;
        set
        {
            this.RaiseAndSetIfChanged(ref _showVolumes, value);
            UpdateVisibility();
        }
    }

    public bool ShowTriggers
    {
        get => _showTriggers;
        set
        {
            this.RaiseAndSetIfChanged(ref _showTriggers, value);
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
    public bool HasSelectedCollision => SelectedCollision != null;
    public bool HasSelectedPath => SelectedPath != null;
    public bool HasSelectedMesh => SelectedMesh != null;
    public bool HasSelectedVolume => SelectedVolume != null;
    public bool HasSelectedTrigger => SelectedTrigger != null;
    public bool HasSelectedAnyObject => HasSelectedObject || HasSelectedCollision || HasSelectedPath || HasSelectedMesh || HasSelectedVolume || HasSelectedTrigger;
    
    /// <summary>
    /// Gets triggers that are related to the currently selected volume's enter events
    /// </summary>
    public List<TriggerVisualizationObject> SelectedVolumeEnterTriggers
    {
        get
        {
            if (SelectedVolume?.EnterEvents == null || !SelectedVolume.EnterEvents.Any())
                return new List<TriggerVisualizationObject>();
                
            var relatedTriggers = new List<TriggerVisualizationObject>();
            foreach (var enterEvent in SelectedVolume.EnterEvents)
            {
                relatedTriggers.AddRange(FindTriggersForEvent(enterEvent));
            }
            return relatedTriggers.Distinct().ToList();
        }
    }
    
    /// <summary>
    /// Gets triggers that are related to the currently selected volume's exit events
    /// </summary>
    public List<TriggerVisualizationObject> SelectedVolumeExitTriggers
    {
        get
        {
            if (SelectedVolume?.ExitEvents == null || !SelectedVolume.ExitEvents.Any())
                return new List<TriggerVisualizationObject>();
                
            var relatedTriggers = new List<TriggerVisualizationObject>();
            foreach (var exitEvent in SelectedVolume.ExitEvents)
            {
                relatedTriggers.AddRange(FindTriggersForEvent(exitEvent));
            }
            return relatedTriggers.Distinct().ToList();
        }
    }
    
    /// <summary>
    /// Gets volumes that are related to the currently selected trigger's activate events
    /// </summary>
    public List<VolumeVisualizationObject> SelectedTriggerActivatingVolumes
    {
        get
        {
            if (SelectedTrigger?.ActivateEvents == null || !SelectedTrigger.ActivateEvents.Any())
                return new List<VolumeVisualizationObject>();
                
            var relatedVolumes = new List<VolumeVisualizationObject>();
            foreach (var activateEvent in SelectedTrigger.ActivateEvents)
            {
                relatedVolumes.AddRange(FindVolumesForEvent(activateEvent));
            }
            return relatedVolumes.Distinct().ToList();
        }
    }
    
    /// <summary>
    /// Gets volumes that are related to the currently selected trigger's deactivate events
    /// </summary>
    public List<VolumeVisualizationObject> SelectedTriggerDeactivatingVolumes
    {
        get
        {
            if (SelectedTrigger?.DeactivateEvents == null || !SelectedTrigger.DeactivateEvents.Any())
                return new List<VolumeVisualizationObject>();
                
            var relatedVolumes = new List<VolumeVisualizationObject>();
            foreach (var deactivateEvent in SelectedTrigger.DeactivateEvents)
            {
                relatedVolumes.AddRange(FindVolumesForEvent(deactivateEvent));
            }
            return relatedVolumes.Distinct().ToList();
        }
    }
    
    /// <summary>
    /// Gets volumes that are related to the currently selected trigger's fire events
    /// </summary>
    public List<VolumeVisualizationObject> SelectedTriggerFireVolumes
    {
        get
        {
            if (SelectedTrigger?.FireEvents == null || !SelectedTrigger.FireEvents.Any())
                return new List<VolumeVisualizationObject>();
                
            var relatedVolumes = new List<VolumeVisualizationObject>();
            foreach (var fireEvent in SelectedTrigger.FireEvents)
            {
                relatedVolumes.AddRange(FindVolumesForEvent(fireEvent));
            }
            return relatedVolumes.Distinct().ToList();
        }
    }

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

    // Expose Vector3 components for XAML binding (Zone Objects)
    public float LocationX => SelectedCoreObject?.m_location.X ?? 0f;
    public float LocationY => SelectedCoreObject?.m_location.Y ?? 0f;  
    public float LocationZ => SelectedCoreObject?.m_location.Z ?? 0f;
    public float OrientationX => SelectedCoreObject?.m_orientation.X ?? 0f;
    public float OrientationY => SelectedCoreObject?.m_orientation.Y ?? 0f;
    public float OrientationZ => SelectedCoreObject?.m_orientation.Z ?? 0f;
    
    // Unified transform properties for current selected object
    public float CurrentLocationX 
    {
        get
        {
            if (SelectedCoreObject != null) return SelectedCoreObject.m_location.X;
            if (SelectedCollision != null) return SelectedCollision.X;
            return 0f;
        }
    }
    
    public float CurrentLocationY 
    {
        get
        {
            if (SelectedCoreObject != null) return SelectedCoreObject.m_location.Y;
            if (SelectedCollision != null) return SelectedCollision.Y;
            return 0f;
        }
    }
    
    public float CurrentLocationZ 
    {
        get
        {
            if (SelectedCoreObject != null) return SelectedCoreObject.m_location.Z;
            if (SelectedCollision != null) return SelectedCollision.Z;
            return 0f;
        }
    }
    
    public float CurrentScale 
    {
        get
        {
            if (SelectedCoreObject != null) return SelectedCoreObject.m_fScale;
            if (SelectedCollision != null) return SelectedCollision.Scale;
            return 1f;
        }
    }

    public ICommand LoadZoneCommand { get; }
    public ICommand SaveZoneCommand { get; }
    public ICommand SelectObjectCommand { get; }
    public ICommand SelectCollisionCommand { get; }
    public ICommand SelectPathCommand { get; }
    public ICommand SelectMeshCommand { get; }
    public ICommand SelectVolumeCommand { get; }
    public ICommand SelectTriggerCommand { get; }
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
    
    public void SetScrollViewer(ScrollViewer scrollViewer)
    {
        _scrollViewer = scrollViewer;
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

            var (zoneData, collisionData, sceneFile, spawnData, pathData, nodeData, volumeData, triggerData) = await _zoneDataService.LoadZoneDataAsync(SelectedZone);
            _currentZoneData = zoneData;
            _currentCollisionData = collisionData;
            _currentSceneFile = sceneFile;
            _currentSpawnData = spawnData;
            _currentPathData = pathData;
            _currentNodeData = nodeData;
            _currentVolumeData = volumeData;
            _currentTriggerData = triggerData;
            
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
                
                // Process volume data if available
                if (_currentVolumeData != null)
                {
                    await PopulateVolumeData(_currentVolumeData);
                }
                
                // Process trigger data if available
                if (_currentTriggerData != null)
                {
                    await PopulateTriggerData(_currentTriggerData);
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
        VolumeVisualizationObjects.Clear();
        TriggerVisualizationObjects.Clear();
        
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

    private async Task PopulateVolumeData(WizZoneVolumes volumeData)
    {
        var tempVolumeObjects = new List<VolumeVisualizationObject>();
        
        await Task.Run(() =>
        {
            Console.WriteLine($"PopulateVolumeData called with {volumeData.m_volumes?.Count ?? 0} volumes");
            
            if (volumeData.m_volumes != null)
            {
                foreach (var volume in volumeData.m_volumes)
                {
                    if (volume != null)
                    {
                        var volumeVis = new VolumeVisualizationObject(volume);
                        tempVolumeObjects.Add(volumeVis);
                        
                        // Debug: Log first few volume objects
                        if (tempVolumeObjects.Count <= 5)
                        {
                            Console.WriteLine($"Volume: {volumeVis.Name} at ({volumeVis.X:F1}, {volumeVis.Y:F1}, {volumeVis.Z:F1}) - Type: {volumeVis.PrimitiveType}");
                        }
                    }
                }
            }
            
            Console.WriteLine($"Volume processing complete. Created {tempVolumeObjects.Count} volume visualization objects");
        });
        
        // Update UI collection on UI thread
        foreach (var item in tempVolumeObjects)
            VolumeVisualizationObjects.Add(item);
            
        Console.WriteLine($"VolumeVisualizationObjects updated. Final count: {VolumeVisualizationObjects.Count}");
        
        // Create volume visuals now that we have volume data
        CreateVolumeVisuals();
    }

    private async Task PopulateTriggerData(WizZoneTriggers triggerData)
    {
        var tempTriggerObjects = new List<TriggerVisualizationObject>();
        
        await Task.Run(() =>
        {
            Console.WriteLine($"PopulateTriggerData called with {triggerData.m_triggers?.Count ?? 0} triggers");
            
            if (triggerData.m_triggers != null)
            {
                foreach (var trigger in triggerData.m_triggers)
                {
                    if (trigger != null)
                    {
                        var triggerVis = new TriggerVisualizationObject(trigger);
                        tempTriggerObjects.Add(triggerVis);
                        
                        // Debug: Log first few trigger objects
                        if (tempTriggerObjects.Count <= 5)
                        {
                            Console.WriteLine($"Trigger: {triggerVis.Name} at ({triggerVis.X:F1}, {triggerVis.Y:F1}, {triggerVis.Z:F1}) - Timing: {triggerVis.TimingText}");
                        }
                    }
                }
            }
            
            Console.WriteLine($"Trigger processing complete. Created {tempTriggerObjects.Count} trigger visualization objects");
        });
        
        // Update UI collection on UI thread
        foreach (var item in tempTriggerObjects)
            TriggerVisualizationObjects.Add(item);
            
        Console.WriteLine($"TriggerVisualizationObjects updated. Final count: {TriggerVisualizationObjects.Count}");
        
        // Note: Triggers don't have visual representation in the viewport, they only appear in the scene hierarchy
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
        ZoomLevel = 0.5; // Reset to 50% zoom
        InitializeViewportCenter();
    }
    
    public void InitializeViewportCenter()
    {
        if (_scrollViewer == null) return;
        
        // Use dispatcher to ensure ScrollViewer is properly initialized
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Center the ScrollViewer on game coordinates (0,0)
            // Game coordinate (0,0) is at canvas position (10000, 10000)
            // Get actual viewport size or use reasonable defaults
            var viewportWidth = _scrollViewer.Viewport.Width > 0 ? _scrollViewer.Viewport.Width : 800;
            var viewportHeight = _scrollViewer.Viewport.Height > 0 ? _scrollViewer.Viewport.Height : 600;
            
            // Center by scrolling to that position minus half the viewport size
            var scrollToX = Math.Max(0, 10000 - (viewportWidth / 2));
            var scrollToY = Math.Max(0, 10000 - (viewportHeight / 2));
            
            Console.WriteLine($"Centering viewport: scrolling to ({scrollToX:F1}, {scrollToY:F1}), viewport size: ({viewportWidth:F1}, {viewportHeight:F1})");
            _scrollViewer.Offset = new Vector(scrollToX, scrollToY);
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }
    
    private void UpdateVisualSelection()
    {
        if (_zoneObjectCanvas == null)
            return;
            
        // Update selection highlighting for all zone objects
        foreach (var child in _zoneObjectCanvas.Children.OfType<Border>())
        {
            if (child.Tag is ZoneVisualizationObject obj)
            {
                // Highlight selected object
                if (SelectedObject != null && obj.TemplateID == SelectedObject.TemplateID)
                {
                    child.BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 0)); // Yellow border for selected
                    child.BorderThickness = new Avalonia.Thickness(2);
                }
                else
                {
                    child.BorderBrush = Brushes.White; // Default white border
                    child.BorderThickness = new Avalonia.Thickness(1);
                }
            }
        }
    }

    private void PanUp()
    {
        if (_scrollViewer != null)
        {
            var newOffset = _scrollViewer.Offset + new Vector(0, -PanStep);
            _scrollViewer.Offset = newOffset;
        }
    }

    private void PanDown()
    {
        if (_scrollViewer != null)
        {
            var newOffset = _scrollViewer.Offset + new Vector(0, PanStep);
            _scrollViewer.Offset = newOffset;
        }
    }

    private void PanLeft()
    {
        if (_scrollViewer != null)
        {
            var newOffset = _scrollViewer.Offset + new Vector(-PanStep, 0);
            _scrollViewer.Offset = newOffset;
        }
    }

    private void PanRight()
    {
        if (_scrollViewer != null)
        {
            var newOffset = _scrollViewer.Offset + new Vector(PanStep, 0);
            _scrollViewer.Offset = newOffset;
        }
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
        if (_scrollViewer != null)
        {
            var newOffset = _scrollViewer.Offset + new Vector(-deltaX, -deltaY);
            _scrollViewer.Offset = newOffset;
        }
    }

    public void UpdateCanvasTransform()
    {
        if (_zoneObjectCanvas?.Parent is Canvas mainCanvas)
        {
            // Apply zoom to the main canvas
            var scaleTransform = new ScaleTransform(ZoomLevel, ZoomLevel);
            mainCanvas.RenderTransform = scaleTransform;
        }
        
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
                
                // Add click handler for selection
                border.PointerPressed += (sender, e) => {
                    if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
                    {
                        // Select this object in the hierarchy and properties
                        SelectedObject = obj;
                        e.Handled = true;
                    }
                };
                
                // Set cursor to indicate clickable
                border.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                
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
        
        // Update visual selection highlights
        UpdateVisualSelection();
        
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
        
        // Update volume visibility
        var volumeVisuals = _zoneObjectCanvas.Children
            .OfType<Control>()
            .Where(c => c.Tag is VolumeVisualizationObject)
            .ToList();
            
        foreach (var visual in volumeVisuals)
        {
            visual.IsVisible = ShowVolumes;
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
    
    private void CreateVolumeVisuals()
    {
        if (_zoneObjectCanvas == null)
        {
            Console.WriteLine("Canvas not set, cannot create volume visuals");
            return;
        }
        
        Console.WriteLine($"Creating volume visuals for {VolumeVisualizationObjects.Count} volumes");
        
        foreach (var volume in VolumeVisualizationObjects)
        {
            try
            {
                var visual = CreateVolumeVisual(volume);
                if (visual != null)
                {
                    visual.IsVisible = ShowVolumes;
                    _zoneObjectCanvas.Children.Add(visual);
                    Console.WriteLine($"Added volume visual for '{volume.Name}' to canvas (visible: {visual.IsVisible})");
                }
                else
                {
                    Console.WriteLine($"Failed to create visual for volume '{volume.Name}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating volume visual for '{volume.Name}': {ex.Message}");
            }
        }
        
        Console.WriteLine($"Created volume visuals on canvas");
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
    
    /// <summary>
    /// Finds triggers that match the given event name in their activate or deactivate events
    /// </summary>
    public List<TriggerVisualizationObject> FindTriggersForEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName) || TriggerVisualizationObjects == null)
            return new List<TriggerVisualizationObject>();
            
        return TriggerVisualizationObjects
            .Where(trigger => 
                trigger.ActivateEvents.Contains(eventName) || 
                trigger.DeactivateEvents.Contains(eventName) ||
                trigger.FireEvents.Contains(eventName))
            .ToList();
    }
    
    /// <summary>
    /// Finds volumes that match the given event name in their enter or exit events
    /// </summary>
    public List<VolumeVisualizationObject> FindVolumesForEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName) || VolumeVisualizationObjects == null)
            return new List<VolumeVisualizationObject>();
            
        return VolumeVisualizationObjects
            .Where(volume => 
                volume.EnterEvents.Contains(eventName) || 
                volume.ExitEvents.Contains(eventName))
            .ToList();
    }
    
    /// <summary>
    /// Command to select a trigger from a volume's related triggers
    /// </summary>
    public ReactiveCommand<TriggerVisualizationObject, Unit> SelectRelatedTriggerCommand { get; }
    
    /// <summary>
    /// Command to select a volume from a trigger's related volumes
    /// </summary>
    public ReactiveCommand<VolumeVisualizationObject, Unit> SelectRelatedVolumeCommand { get; }
    
    private void SelectRelatedTrigger(TriggerVisualizationObject trigger)
    {
        if (trigger == null) return;
        
        // Clear other selections
        SelectedObject = null;
        SelectedCollision = null;
        SelectedPath = null;
        SelectedMesh = null;
        SelectedVolume = null;
        
        // Select the trigger
        SelectedTrigger = trigger;
        
        Debug.WriteLine($"Selected related trigger: {trigger.Name}");
    }
    
    private void SelectRelatedVolume(VolumeVisualizationObject volume)
    {
        if (volume == null) return;
        
        // Clear other selections
        SelectedObject = null;
        SelectedCollision = null;
        SelectedPath = null;
        SelectedMesh = null;
        SelectedTrigger = null;
        
        // Select the volume
        SelectedVolume = volume;
        
        Debug.WriteLine($"Selected related volume: {volume.Name}");
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
    
    private Control? CreateVolumeVisual(VolumeVisualizationObject volume)
    {
        // Convert coordinates (same as zone objects)
        var canvasX = 10000.0 + (volume.X * 0.25);
        var canvasY = 10000.0 - (volume.Y * 0.25);
        
        // Use red color as requested
        var volumeBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 0, 0)); // Red
        var volumeStroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(200, 0, 0)); // Darker red for stroke
        
        Control? visual = null;
        
        // Create shape based on primitive type
        var primitiveType = volume.PrimitiveType?.ToLowerInvariant() ?? "unknown";
        switch (primitiveType)
        {
            case "box":
            case "cube":
                visual = CreateVolumeBoxVisual(volume);
                break;
            case "sphere":
            case "ball":
                visual = CreateVolumeSphereVisual(volume);
                break;
            case "cylinder":
                visual = CreateVolumeCylinderVisual(volume);
                break;
            default:
                // Fallback to box for unknown types
                visual = CreateVolumeBoxVisual(volume);
                break;
        }
        
        if (visual != null)
        {
            // Position on canvas
            Canvas.SetLeft(visual, canvasX);
            Canvas.SetTop(visual, canvasY);
            
            // Add tooltip
            ToolTip.SetTip(visual, $"{volume.Name} ({volume.PrimitiveType})\n{volume.DimensionsText}\nAt: ({volume.X:F1}, {volume.Y:F1}, {volume.Z:F1})\n{volume.EventsText}");
            
            // Tag for identification and selection
            visual.Tag = volume;
            
            // Add click handler for selection
            visual.PointerPressed += (sender, e) => {
                if (e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
                {
                    SelectedVolume = volume;
                    e.Handled = true;
                }
            };
            
            // Set cursor to indicate clickable
            visual.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        }
        
        return visual;
    }
    
    private Control CreateVolumeBoxVisual(VolumeVisualizationObject volume)
    {
        var width = Math.Max(10, volume.Width * 0.25);
        var height = Math.Max(10, volume.Length * 0.25);
        
        // Create a canvas to hold the shape and diagonal lines
        var canvas = new Canvas
        {
            Width = width,
            Height = height
        };
        
        // Create the rectangle outline
        var rectangle = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brushes.Transparent, // No solid fill
            Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 0, 0)),
            StrokeThickness = 2
        };
        canvas.Children.Add(rectangle);
        
        // Add diagonal lines for shading
        AddDiagonalLines(canvas, width, height);
        
        return canvas;
    }
    
    private Control CreateVolumeSphereVisual(VolumeVisualizationObject volume)
    {
        var diameter = Math.Max(10, volume.Radius * 2 * 0.25);
        
        // Create a canvas to hold the shape and diagonal lines
        var canvas = new Canvas
        {
            Width = diameter,
            Height = diameter
        };
        
        // Create the ellipse outline
        var ellipse = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = Brushes.Transparent, // No solid fill
            Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 0, 0)),
            StrokeThickness = 2
        };
        canvas.Children.Add(ellipse);
        
        // Add diagonal lines for shading (clipped to circle)
        AddDiagonalLinesClipped(canvas, diameter, diameter, true);
        
        return canvas;
    }
    
    private Control CreateVolumeCylinderVisual(VolumeVisualizationObject volume)
    {
        // For 2D view, cylinder appears as circle
        var diameter = Math.Max(10, volume.Radius * 2 * 0.25);
        
        // Create a canvas to hold the shape and diagonal lines
        var canvas = new Canvas
        {
            Width = diameter,
            Height = diameter
        };
        
        // Create the ellipse outline
        var ellipse = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = Brushes.Transparent, // No solid fill
            Stroke = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 0, 0)),
            StrokeThickness = 2
        };
        canvas.Children.Add(ellipse);
        
        // Add diagonal lines for shading (clipped to circle)
        AddDiagonalLinesClipped(canvas, diameter, diameter, true);
        
        return canvas;
    }
    
    private void AddDiagonalLines(Canvas canvas, double width, double height)
    {
        const double lineSpacing = 8; // Spacing between diagonal lines
        var lineColor = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 0, 0));
        
        // Add diagonal lines from top-left to bottom-right
        for (double offset = -height; offset < width + height; offset += lineSpacing)
        {
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(Math.Max(0, offset), Math.Max(0, -offset)),
                EndPoint = new Avalonia.Point(Math.Min(width, offset + height), Math.Min(height, height - offset)),
                Stroke = lineColor,
                StrokeThickness = 1,
                Opacity = 0.6
            };
            
            canvas.Children.Add(line);
        }
    }
    
    private void AddDiagonalLinesClipped(Canvas canvas, double width, double height, bool isCircle)
    {
        const double lineSpacing = 8; // Spacing between diagonal lines
        var lineColor = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 0, 0));
        
        var centerX = width / 2;
        var centerY = height / 2;
        var radius = Math.Min(width, height) / 2;
        
        // Add diagonal lines, but clip them to the circle if needed
        for (double offset = -height; offset < width + height; offset += lineSpacing)
        {
            var startX = Math.Max(0, offset);
            var startY = Math.Max(0, -offset);
            var endX = Math.Min(width, offset + height);
            var endY = Math.Min(height, height - offset);
            
            if (isCircle)
            {
                // Clip line to circle bounds (simplified approach)
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Avalonia.Point(startX, startY),
                    EndPoint = new Avalonia.Point(endX, endY),
                    Stroke = lineColor,
                    StrokeThickness = 1,
                    Opacity = 0.6
                };
                
                // Apply a circular clip geometry
                line.Clip = new EllipseGeometry(new Avalonia.Rect(0, 0, width, height));
                canvas.Children.Add(line);
            }
            else
            {
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Avalonia.Point(startX, startY),
                    EndPoint = new Avalonia.Point(endX, endY),
                    Stroke = lineColor,
                    StrokeThickness = 1,
                    Opacity = 0.6
                };
                
                canvas.Children.Add(line);
            }
        }
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
        ClearAllSelections();
        
        SelectedObject = visualObj;
        
        // Find the corresponding CoreObjectInfo
        if (_currentZoneData?.m_objectList != null)
        {
            SelectedCoreObject = _currentZoneData.m_objectList
                .FirstOrDefault(obj => obj != null && (ulong)obj.m_templateID == visualObj.TemplateID);
        }
        
        NotifySelectionChanged();
    }
    
    private void SelectCollision(CollisionVisualizationObject collision)
    {
        ClearAllSelections();
        SelectedCollision = collision;
        NotifySelectionChanged();
    }
    
    private void SelectPath(PathVisualizationObject path)
    {
        ClearAllSelections();
        SelectedPath = path;
        NotifySelectionChanged();
    }
    
    private void SelectMesh(NifMeshVisualizationObject mesh)
    {
        ClearAllSelections();
        SelectedMesh = mesh;
        NotifySelectionChanged();
    }
    
    private void SelectVolume(VolumeVisualizationObject volume)
    {
        ClearAllSelections();
        SelectedVolume = volume;
        NotifySelectionChanged();
    }
    
    private void SelectTrigger(TriggerVisualizationObject trigger)
    {
        ClearAllSelections();
        SelectedTrigger = trigger;
        NotifySelectionChanged();
    }
    
    private void ClearAllSelections()
    {
        SelectedObject = null;
        SelectedCoreObject = null;
        SelectedTemplate = null;
        SelectedCollision = null;
        SelectedPath = null;
        SelectedMesh = null;
        SelectedVolume = null;
        SelectedTrigger = null;
    }
    
    private void NotifySelectionChanged()
    {
        // Notify UI of property changes for all transform properties
        this.RaisePropertyChanged(nameof(HasSelectedObject));
        this.RaisePropertyChanged(nameof(HasSelectedCollision));
        this.RaisePropertyChanged(nameof(HasSelectedPath));
        this.RaisePropertyChanged(nameof(HasSelectedMesh));
        this.RaisePropertyChanged(nameof(HasSelectedVolume));
        this.RaisePropertyChanged(nameof(HasSelectedTrigger));
        this.RaisePropertyChanged(nameof(HasSelectedAnyObject));
        this.RaisePropertyChanged(nameof(LocationX));
        this.RaisePropertyChanged(nameof(LocationY));
        this.RaisePropertyChanged(nameof(LocationZ));
        this.RaisePropertyChanged(nameof(OrientationX));
        this.RaisePropertyChanged(nameof(OrientationY));
        this.RaisePropertyChanged(nameof(OrientationZ));
        this.RaisePropertyChanged(nameof(CurrentLocationX));
        this.RaisePropertyChanged(nameof(CurrentLocationY));
        this.RaisePropertyChanged(nameof(CurrentLocationZ));
        this.RaisePropertyChanged(nameof(CurrentScale));
        this.RaisePropertyChanged(nameof(SelectedTemplate));
    }
    
    /// <summary>
    /// Loads the template data for the given CoreObjectInfo using the TemplateManifestService
    /// </summary>
    /// <param name="coreObject">The core object to load template for</param>
    /// <returns>The loaded template, or null if not found or failed to load</returns>
    private async Task<PropertyClass?> LoadTemplateAsync(CoreObjectInfo coreObject)
    {
        try
        {
            if (coreObject == null)
                return null;

            // First, ensure template manifest is loaded
            var manifestService = TemplateManifestService.Instance;
            if (!manifestService.IsLoaded)
            {
                // Try to load template manifest from Root.wad
                var templateManifestData = await RootWadService.Instance.GetFileAsync("TemplateManifest.xml");
                if (templateManifestData == null || !templateManifestData.HasValue)
                {
                    Console.WriteLine("Failed to load TemplateManifest.xml from Root.wad");
                    return null;
                }
                
                if (!manifestService.LoadFromFileData(templateManifestData.Value))
                {
                    Console.WriteLine("Failed to parse TemplateManifest.xml");
                    return null;
                }
            }

            // Find the template location by template ID
            var templateLocations = manifestService.GetAllTemplateLocations();
            var templateLocation = templateLocations.FirstOrDefault(t => t.m_id == coreObject.m_templateID);
            
            if (templateLocation == null)
            {
                Console.WriteLine($"Template with ID {coreObject.m_templateID} not found in manifest");
                return null;
            }

            Console.WriteLine($"Found template: {templateLocation.m_filename} for template ID {coreObject.m_templateID}");

            // Load the template file from Root.wad
            var templateData = await RootWadService.Instance.GetFileAsync(templateLocation.m_filename);
            if (templateData == null || !templateData.HasValue)
            {
                Console.WriteLine($"Failed to load template file: {templateLocation.m_filename}");
                return null;
            }

            // Try to deserialize as GameObjectTemplate first, then fallback to PropertyClass
            var serializer = new BindSerializer();
            
            // First try GameObjectTemplate specifically
            if (serializer.Deserialize<GameObjectTemplate>(templateData.Value.ToArray(), 1, out var gameObjectTemplate) && gameObjectTemplate != null)
            {
                Console.WriteLine($"Successfully loaded GameObjectTemplate: {gameObjectTemplate.GetType().Name} for {coreObject.m_zoneTag ?? "Unknown"}");
                return gameObjectTemplate;
            }
            
            // Fallback to generic PropertyClass deserialization
            if (serializer.Deserialize<PropertyClass>(templateData.Value.ToArray(), 1, out var template) && template != null)
            {
                Console.WriteLine($"Successfully loaded template as PropertyClass: {template.GetType().Name} for {coreObject.m_zoneTag ?? "Unknown"}");
                return template;
            }
            
            Console.WriteLine($"Failed to deserialize template from {templateLocation.m_filename}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading template for object {coreObject.m_zoneTag}: {ex.Message}");
            return null;
        }
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
    
    /// <summary>
    /// Resolves a locale string reference (e.g., "WC-NPCs_00002516") to its English text.
    /// </summary>
    /// <param name="localeReference">The locale reference string.</param>
    /// <returns>The resolved English text, or null if not found.</returns>
    private string? ResolveLocaleString(string localeReference)
    {
        if (string.IsNullOrEmpty(localeReference) || !LocaleService.Instance.IsLoaded)
        {
            return null;
        }

        // Split the locale reference into category and key (e.g., "WC-NPCs_00002516")
        var underscoreIndex = localeReference.LastIndexOf('_');
        if (underscoreIndex == -1)
        {
            return null; // Invalid format
        }

        var category = localeReference.Substring(0, underscoreIndex);
        var key = localeReference.Substring(underscoreIndex + 1);

        // Pad the key to 8 digits if it's not already and is all numeric
        if (key.Length < 8 && key.All(char.IsDigit))
        {
            key = key.PadLeft(8, '0');
        }

        return LocaleService.Instance.GetString(category, key);
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

public class BehaviorWrapper
{
    public object? OriginalBehavior { get; }
    public string TypeName { get; }
    public string FormattedJson { get; }
    
    public BehaviorWrapper(object? behavior)
    {
        OriginalBehavior = behavior;
        
        if (behavior == null)
        {
            TypeName = "Unknown";
            FormattedJson = "// Behavior object is null";
            return;
        }
        
        TypeName = behavior.GetType().Name;
        
        try
        {
            // Try to serialize as JSON with pretty formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            FormattedJson = JsonSerializer.Serialize(behavior, behavior.GetType(), options);
        }
        catch (Exception ex)
        {
            // If JSON serialization fails, try a simple property-by-property approach
            try
            {
                var properties = behavior.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"_type\": \"{behavior.GetType().Name}\",");
                
                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(behavior);
                        var valueStr = value?.ToString() ?? "null";
                        
                        // Escape quotes in the value
                        valueStr = valueStr.Replace("\"", "\\\"");
                        
                        sb.AppendLine($"  \"{prop.Name}\": \"{valueStr}\",");
                    }
                    catch (Exception propEx)
                    {
                        sb.AppendLine($"  \"{prop.Name}\": \"<Error: {propEx.Message}>\",");
                    }
                }
                
                // Remove trailing comma and close
                var result = sb.ToString().TrimEnd(',', '\n', '\r');
                if (result.EndsWith(","))
                {
                    result = result.Substring(0, result.Length - 1) + "\n";
                }
                result += "\n}";
                FormattedJson = result;
            }
            catch (Exception fallbackEx)
            {
                // Final fallback to ToString
                FormattedJson = $"// JSON Serialization Failed: {ex.Message}\n// Property Reflection Failed: {fallbackEx.Message}\n\n{behavior}";
            }
        }
    }
}
