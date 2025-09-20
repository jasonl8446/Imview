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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Imcodec.IO;
using Imcodec.Wad;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.ObjectProperty;
using Imcodec.BCD;
using Imview.Core.Models;
using WizardTea.Core;

namespace Imview.Core.Services;

public class ZoneDataService
{
    private const string ZoneDataFileName = "gamedata.bin";
    private const string CollisionDataFileName = "collision.bcd";
    private const string SpawnDataFileName = "spawnData.xml";
    private const string PathDataFileName = "pathData.xml";
    private const string NodeDataFileName = "pathNodeData.bin";
    private const string VolumesDataFileName = "volumes.xml";
    private const string TriggersDataFileName = "triggers.xml";
    private const string AccessPassFileName = "AccessPass.xml";
    private readonly ClientFileService _clientFileService;
    private readonly RootWadService _rootWadService;
    private readonly HttpClient _httpClient;
    
    public ZoneDataService()
    {
        _clientFileService = new ClientFileService();
        _rootWadService = RootWadService.Instance;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }
    
    public async Task<(WizZoneData? ZoneData, Bcd? CollisionData, NifFile? SceneFile, SpawnManager? SpawnData, PathTemplateList? PathData, NodeTemplateList? NodeData, WizZoneVolumes? VolumeData, WizZoneTriggers? TriggerData)> LoadZoneDataAsync(string zoneName)
    {
        try
        {
            // Determine zone WAD filename - zones use forward slashes in AccessPass.xml but hyphens in WAD names
            var zoneWadName = zoneName.Replace('/', '-') + ".wad";
            Console.WriteLine($"Loading zone '{zoneName}' -> WAD file '{zoneWadName}'");
            
            // First, try to get the zone WAD from local cache
            var zoneWadData = await GetZoneWadDataAsync(zoneWadName);
            if (zoneWadData == null)
            {
                throw new FileNotFoundException($"Zone WAD '{zoneWadName}' is not available. The zone may not exist in the current client revision or download may have failed.", zoneWadName);
            }
            
            // Parse the zone WAD
            using var zoneWadStream = new MemoryStream(zoneWadData);
            var zoneArchive = ArchiveParser.Parse(zoneWadStream);
            
            if (zoneArchive == null)
            {
                throw new InvalidOperationException($"Failed to parse zone WAD '{zoneWadName}'");
            }
            
            // Extract gamedata.bin from the zone WAD
            var gameDataFile = zoneArchive.OpenFile(ZoneDataFileName);
            if (gameDataFile == null)
            {
                throw new InvalidOperationException($"'{ZoneDataFileName}' not found in zone WAD '{zoneWadName}'");
            }
            
            // Deserialize the zone data
            var bindSerializer = new BindSerializer();
            var gameDataBytes = gameDataFile.Value.ToArray();
            
            WizZoneData? zoneData = null;
            if (!bindSerializer.Deserialize<WizZoneData>(gameDataBytes, 1, out zoneData))
            {
                throw new InvalidOperationException($"Failed to deserialize zone data from '{ZoneDataFileName}' in '{zoneWadName}'");
            }
            
            // Extract collision.bcd from the zone WAD (optional - not all zones may have collision data)
            Bcd? collisionData = null;
            var collisionFile = zoneArchive.OpenFile(CollisionDataFileName);
            if (collisionFile != null)
            {
                try
                {
                    using var collisionStream = new MemoryStream(collisionFile.Value.ToArray());
                    collisionData = Bcd.Parse(collisionStream);
                    Console.WriteLine($"Loaded {collisionData.Collisions.Count} collision objects from '{CollisionDataFileName}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse collision data from '{CollisionDataFileName}': {ex.Message}");
                    // Continue without collision data - it's not critical
                }
            }
            else
            {
                Console.WriteLine($"No '{CollisionDataFileName}' found in zone WAD '{zoneWadName}' - zone may not have collision data");
            }
            
            // Extract .nif scene file from the zone WAD (optional - use gamebryoSceneFileName from zone data)
            NifFile? sceneFile = null;
            if (zoneData != null && !string.IsNullOrEmpty(zoneData.m_gamebryoSceneFileName))
            {
                try
                {
                    var nifFileName = zoneData.m_gamebryoSceneFileName;
                    Console.WriteLine($"Looking for NIF scene file: '{nifFileName}'");
                    
                    var nifFile = zoneArchive.OpenFile(nifFileName);
                    if (nifFile != null)
                    {
                        using var nifStream = new MemoryStream(nifFile.Value.ToArray());
                        var wizardTeaStream = new NifStream(nifStream);
                        sceneFile = new NifFile(wizardTeaStream);
                        Console.WriteLine($"Successfully loaded NIF scene file '{nifFileName}' with {sceneFile.Blocks.Length} blocks");
                    }
                    else
                    {
                        Console.WriteLine($"NIF scene file '{nifFileName}' not found in zone WAD '{zoneWadName}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse NIF scene file '{zoneData.m_gamebryoSceneFileName}': {ex.Message}");
                    // Continue without scene file - it's not critical for basic zone functionality
                }
            }
            else
            {
                Console.WriteLine($"No gamebryoSceneFileName specified in zone data or zone data is null");
            }
            
            // Load spawn data (critical for creature path visualization)
            SpawnManager? spawnData = null;
            var spawnFile = zoneArchive.OpenFile(SpawnDataFileName);
            if (spawnFile != null)
            {
                try
                {
                    var spawnDataBytes = spawnFile.Value.ToArray();
                    Console.WriteLine($"Found '{SpawnDataFileName}' file, size: {spawnDataBytes.Length} bytes");
                    
                    if (!bindSerializer.Deserialize<SpawnManager>(spawnDataBytes, 1, out spawnData))
                    {
                        Console.WriteLine($"ERROR: Failed to deserialize spawn data from '{SpawnDataFileName}' - trying alternative methods");
                        
                        // Try alternative deserialization strategies
                        var altSerializer = new BindSerializer();
                        if (altSerializer.Deserialize<SpawnManager>(spawnDataBytes, out spawnData))
                        {
                            Console.WriteLine($"SUCCESS: Alternative deserialization worked for spawn data");
                        }
                        else
                        {
                            Console.WriteLine($"CRITICAL: All spawn deserialization methods failed");
                            spawnData = null;
                        }
                    }
                    
                    if (spawnData?.m_spawners != null)
                    {
                        Console.WriteLine($"SUCCESS: Loaded {spawnData.m_spawners.Count} spawn objects from '{SpawnDataFileName}'");
                        
                        // Analyze spawn data structure for debugging
                        int totalSpawnItems = 0;
                        int spawnsWithPaths = 0;
                        
                        foreach (var spawner in spawnData.m_spawners.Take(5)) // Sample first 5 for debugging
                        {
                            if (spawner?.m_spawnList != null)
                            {
                                totalSpawnItems += spawner.m_spawnList.Count;
                                
                                foreach (var spawnItem in spawner.m_spawnList.Take(3)) // Sample first 3 per spawner
                                {
                                    if (spawnItem?.m_objectInfo != null)
                                    {
                                        var objInfo = spawnItem.m_objectInfo;
                                        Console.WriteLine($"  SpawnItem: ID={objInfo.m_nObjectID}, PathID={objInfo.m_pathID}, ZoneTag='{objInfo.m_zoneTag}', Location=({objInfo.m_location.X:F2}, {objInfo.m_location.Y:F2}, {objInfo.m_location.Z:F2})");
                                        if (objInfo.m_pathID != 0)
                                        {
                                            spawnsWithPaths++;
                                        }
                                    }
                                }
                            }
                        }
                        
                        Console.WriteLine($"Spawn analysis: {totalSpawnItems} total spawn items, {spawnsWithPaths} have non-zero path IDs");
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: SpawnManager loaded but m_spawners is null or empty");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CRITICAL ERROR: Exception while parsing spawn data from '{SpawnDataFileName}': {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    spawnData = null;
                }
            }
            else
            {
                Console.WriteLine($"WARNING: '{SpawnDataFileName}' not found in zone WAD '{zoneWadName}'");
                Console.WriteLine("This will result in 0 spawn creatures being displayed");
            }
            
            // Load path data (optional)
            PathTemplateList? pathData = null;
            var pathFile = zoneArchive.OpenFile(PathDataFileName);
            if (pathFile != null)
            {
                try
                {
                    var pathDataBytes = pathFile.Value.ToArray();
                    if (!bindSerializer.Deserialize<PathTemplateList>(pathDataBytes, 1, out pathData))
                    {
                        Console.WriteLine($"Warning: Failed to deserialize path data from '{PathDataFileName}'");
                    }
                    else
                    {
                        Console.WriteLine($"Loaded path data with {pathData.m_pathList?.Count ?? 0} path templates from '{PathDataFileName}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse path data from '{PathDataFileName}': {ex.Message}");
                    // Continue without path data - it's not critical
                }
            }
            else
            {
                Console.WriteLine($"No '{PathDataFileName}' found in zone WAD '{zoneWadName}' - zone may not have path data");
            }
            
            // Load node data (critical for path visualization)
            NodeTemplateList? nodeData = null;
            Console.WriteLine($"Loading node data from '{NodeDataFileName}' in zone WAD '{zoneWadName}'");
            var nodeFile = zoneArchive.OpenFile(NodeDataFileName);
            if (nodeFile != null)
            {
                Console.WriteLine($"Found '{NodeDataFileName}' file, size: {nodeFile.Value.Length} bytes");
                try
                {
                    var nodeDataBytes = nodeFile.Value.ToArray();
                    Console.WriteLine($"Raw node data: {nodeDataBytes.Length} bytes");
                    Console.WriteLine($"First 16 bytes (hex): {BitConverter.ToString(nodeDataBytes.Take(16).ToArray())}");
                    
                    // Try multiple deserialization strategies
                    bool deserializeSuccess = false;
                    
                    // Strategy 1: Standard deserialization with version 1
                    Console.WriteLine("Attempting deserialization strategy 1: BindSerializer with version 1");
                    if (bindSerializer.Deserialize<NodeTemplateList>(nodeDataBytes, 1, out nodeData))
                    {
                        deserializeSuccess = true;
                        Console.WriteLine($"Strategy 1 SUCCESS: {nodeData?.m_nodeList?.Count ?? 0} nodes");
                    }
                    
                    // Strategy 2: Try without version parameter if strategy 1 failed
                    if (!deserializeSuccess)
                    {
                        Console.WriteLine("Strategy 1 failed, attempting strategy 2: BindSerializer with default version");
                        try
                        {
                            var freshSerializer = new BindSerializer();
                            if (freshSerializer.Deserialize<NodeTemplateList>(nodeDataBytes, (Imcodec.ObjectProperty.PropertyFlags)0, out nodeData))
                            {
                                deserializeSuccess = true;
                                Console.WriteLine($"Strategy 2 SUCCESS: {nodeData?.m_nodeList?.Count ?? 0} nodes");
                            }
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"Strategy 2 FAILED: {ex2.Message}");
                        }
                    }
                    
                    // Strategy 3: Try different version numbers
                    if (!deserializeSuccess)
                    {
                        Console.WriteLine("Attempting strategy 3: trying different version numbers");
                        for (int version = 0; version <= 3; version++)
                        {
                            try
                            {
                                var versionSerializer = new BindSerializer();
                                if (versionSerializer.Deserialize<NodeTemplateList>(nodeDataBytes, (Imcodec.ObjectProperty.PropertyFlags)version, out nodeData))
                                {
                                    deserializeSuccess = true;
                                    Console.WriteLine($"Strategy 3 SUCCESS with version {version}: {nodeData?.m_nodeList?.Count ?? 0} nodes");
                                    break;
                                }
                            }
                            catch (Exception ex3)
                            {
                                Console.WriteLine($"Strategy 3 version {version} failed: {ex3.Message}");
                            }
                        }
                    }
                    
                    if (!deserializeSuccess)
                    {
                        Console.WriteLine($"ERROR: All deserialization strategies failed for '{NodeDataFileName}'");
                        Console.WriteLine("This will result in 0 nodes being displayed in the zone editor");
                        nodeData = null;
                    }
                    else
                    {
                        Console.WriteLine($"FINAL SUCCESS: Loaded {nodeData?.m_nodeList?.Count ?? 0} node objects from '{NodeDataFileName}'");
                        if (nodeData?.m_nodeList != null)
                        {
                            Console.WriteLine($"Sample node data (first 3 nodes):");
                            for (int i = 0; i < Math.Min(3, nodeData.m_nodeList.Count); i++)
                            {
                                var node = nodeData.m_nodeList[i];
                                if (node != null)
                                {
                                    Console.WriteLine($"  Node {i}: ID={node.m_id}, Location=({node.m_location.X:F2}, {node.m_location.Y:F2}, {node.m_location.Z:F2})");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CRITICAL ERROR: Exception while parsing node data from '{NodeDataFileName}': {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    nodeData = null;
                }
            }
            else
            {
                Console.WriteLine($"WARNING: '{NodeDataFileName}' not found in zone WAD '{zoneWadName}'");
                Console.WriteLine("This will result in 0 nodes being displayed - paths will not be visualizable");
                
                // List all files in the WAD for debugging
                Console.WriteLine($"Available files in zone WAD '{zoneWadName}':");
                var allFiles = zoneArchive.Files.Keys.ToList();
                foreach (var file in allFiles.Take(20))
                {
                    Console.WriteLine($"  - {file}");
                }
                if (allFiles.Count > 20)
                {
                    Console.WriteLine($"  ... and {allFiles.Count - 20} more files");
                }
                
                // Look for alternative node file names
                var possibleNodeFiles = allFiles.Where(f => 
                    f.Contains("node", StringComparison.OrdinalIgnoreCase) ||
                    f.Contains("path", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)).ToList();
                
                if (possibleNodeFiles.Any())
                {
                    Console.WriteLine("Possible alternative node/path files found:");
                    foreach (var file in possibleNodeFiles)
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }
            }
            
            // Load volume data (optional)
            WizZoneVolumes? volumeData = null;
            var volumeFile = zoneArchive.OpenFile(VolumesDataFileName);
            if (volumeFile != null)
            {
                try
                {
                    var volumeDataBytes = volumeFile.Value.ToArray();
                    if (!bindSerializer.Deserialize<WizZoneVolumes>(volumeDataBytes, 1, out volumeData))
                    {
                        Console.WriteLine($"Warning: Failed to deserialize volume data from '{VolumesDataFileName}'");
                    }
                    else
                    {
                        Console.WriteLine($"Loaded volume data with {volumeData.m_volumes?.Count ?? 0} volumes from '{VolumesDataFileName}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse volume data from '{VolumesDataFileName}': {ex.Message}");
                    // Continue without volume data - it's not critical
                }
            }
            else
            {
                Console.WriteLine($"No '{VolumesDataFileName}' found in zone WAD '{zoneWadName}' - zone may not have volume data");
            }
            
            // Load trigger data (optional)
            WizZoneTriggers? triggerData = null;
            var triggerFile = zoneArchive.OpenFile(TriggersDataFileName);
            if (triggerFile != null)
            {
                try
                {
                    var triggerDataBytes = triggerFile.Value.ToArray();
                    if (!bindSerializer.Deserialize<WizZoneTriggers>(triggerDataBytes, 1, out triggerData))
                    {
                        Console.WriteLine($"Warning: Failed to deserialize trigger data from '{TriggersDataFileName}'");
                    }
                    else
                    {
                        Console.WriteLine($"Loaded trigger data with {triggerData.m_triggers?.Count ?? 0} triggers from '{TriggersDataFileName}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse trigger data from '{TriggersDataFileName}': {ex.Message}");
                    // Continue without trigger data - it's not critical
                }
            }
            else
            {
                Console.WriteLine($"No '{TriggersDataFileName}' found in zone WAD '{zoneWadName}' - zone may not have trigger data");
            }
            
            return (zoneData, collisionData, sceneFile, spawnData, pathData, nodeData, volumeData, triggerData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load zone data for '{zoneName}': {ex.Message}", ex);
        }
    }
    
    public async Task<List<string>> GetAvailableZonesAsync()
    {
        try
        {
            // Load Root.wad if not already loaded
            if (!_rootWadService.IsLoaded)
            {
                Console.WriteLine("Root.wad not loaded, attempting to load from cache...");
                var loadSuccess = await _rootWadService.LoadFromCacheAsync();
                if (!loadSuccess)
                {
                    Console.WriteLine("Failed to load Root.wad from cache");
                    return new List<string>();
                }
            }
            
            // Get and parse AccessPass.xml from Root.wad
            var accessPassData = await _rootWadService.GetFileAsync(AccessPassFileName);
            if (accessPassData == null)
            {
                throw new InvalidOperationException("AccessPass.xml not found in Root.wad");
            }
            
            var zones = ParseAccessPass(accessPassData.Value.ToArray());
            Console.WriteLine($"Found {zones.Count} zones in AccessPass.xml");
            Console.WriteLine($"First 10 zones: {string.Join(", ", zones.Take(10))}");
            return zones.OrderBy(name => name).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover available zones: {ex.Message}", ex);
        }
    }
    
    private List<string> ParseAccessPass(byte[] accessPassData)
    {
        var zones = new List<string>();
        
        try
        {
            using var stream = new MemoryStream(accessPassData);
            var doc = new XmlDocument();
            doc.Load(stream);
            
            foreach (XmlNode zoneNode in doc.GetElementsByTagName("Zone"))
            {
                var zoneName = zoneNode.InnerText?.Trim();
                if (!string.IsNullOrEmpty(zoneName))
                {
                    zones.Add(zoneName);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse AccessPass.xml: {ex.Message}", ex);
        }
        
        return zones;
    }
    
    private async Task<byte[]?> GetZoneWadDataAsync(string zoneWadName)
    {
        // First, try the local cache (check if the zone WAD is already downloaded)
        var cacheDir = _clientFileService.GetCacheDirectory();
        var selectedRevision = _clientFileService.GetSelectedRevision();
        
        Console.WriteLine($"Cache directory: {cacheDir}");
        Console.WriteLine($"Selected revision: {selectedRevision}");
        
        if (!string.IsNullOrEmpty(selectedRevision))
        {
            // Check both with and without Data/GameData prefix since ClientFileService might flatten the path
            var cachedWadPath = Path.Combine(cacheDir, selectedRevision, zoneWadName);
            var cachedWadPathWithPrefix = Path.Combine(cacheDir, selectedRevision, "Data", "GameData", zoneWadName);
            
            Console.WriteLine($"Checking cache path: {cachedWadPath}");
            Console.WriteLine($"Checking cache path with prefix: {cachedWadPathWithPrefix}");
            
            if (File.Exists(cachedWadPath))
            {
                Console.WriteLine($"Found cached WAD file: {cachedWadPath}");
                return await File.ReadAllBytesAsync(cachedWadPath);
            }
            else if (File.Exists(cachedWadPathWithPrefix))
            {
                Console.WriteLine($"Found cached WAD file with prefix: {cachedWadPathWithPrefix}");
                return await File.ReadAllBytesAsync(cachedWadPathWithPrefix);
            }
            else
            {
                Console.WriteLine($"WAD file not found in either cache location");
            }
        }
        
        // If not cached, try to download it from the patch server
        Console.WriteLine($"Attempting to download zone WAD: {zoneWadName}");
        try
        {
            return await DownloadZoneWadAsync(zoneWadName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download attempt failed for zone WAD '{zoneWadName}': {ex.Message}");
            // Return null to indicate the zone is not available, rather than throwing
            return null;
        }
    }
    
    private async Task<byte[]?> DownloadZoneWadAsync(string zoneWadName)
    {
        try
        {
            var selectedRevision = _clientFileService.GetSelectedRevision();
            if (string.IsNullOrEmpty(selectedRevision))
            {
                Console.WriteLine("No client revision selected.");
                throw new InvalidOperationException("No client revision selected. Please configure client files first.");
            }
            
            Console.WriteLine($"Fetching file list for revision: {selectedRevision}");
            // Use the existing ClientFileService logic to get the file list and download
            var fileList = await _clientFileService.FetchFileListAsync(selectedRevision);
            Console.WriteLine($"File list contains {fileList.Records.Count} files");
            
            // Let's see what zone WAD files are available
            var allWadFiles = fileList.Records.Where(r => r.SourceFileName.EndsWith(".wad", StringComparison.OrdinalIgnoreCase)).ToList();
            Console.WriteLine($"Available WAD files: {string.Join(", ", allWadFiles.Take(10).Select(r => r.SourceFileName))}");
            
            // Zone WADs are located under Data/GameData/ path
            var fullZoneWadPath = $"Data/GameData/{zoneWadName}";
            Console.WriteLine($"Looking for zone WAD with full path: {fullZoneWadPath}");
            
            var zoneFileRecord = fileList.Records.FirstOrDefault(r => 
                r.SourceFileName.Equals(fullZoneWadPath, StringComparison.OrdinalIgnoreCase));
            
            if (zoneFileRecord == null)
            {
                Console.WriteLine($"Zone WAD '{fullZoneWadPath}' not found in file list");
                // Zone WAD not found in file list - might not exist for this revision
                return null;
            }
            
            Console.WriteLine($"Found zone file record: {zoneFileRecord.SourceFileName}");
            
            // Download the zone WAD using the existing download infrastructure
            var localFilePath = await _clientFileService.DownloadFileAsync(zoneFileRecord, selectedRevision);
            Console.WriteLine($"Downloaded to: {localFilePath}");
            
            if (File.Exists(localFilePath))
            {
                var fileSize = new FileInfo(localFilePath).Length;
                Console.WriteLine($"Downloaded WAD file size: {fileSize} bytes");
                return await File.ReadAllBytesAsync(localFilePath);
            }
            
            Console.WriteLine($"Downloaded file does not exist at: {localFilePath}");
            return null;
        }
        catch (Exception ex)
        {
            // If download fails, return null - zone might not be available
            Console.WriteLine($"Failed to download zone WAD '{zoneWadName}': {ex.Message}");
            Console.WriteLine($"Exception stack trace: {ex.StackTrace}");
            return null;
        }
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}