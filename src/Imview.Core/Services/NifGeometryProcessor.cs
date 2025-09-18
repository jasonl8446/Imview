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
using System.Linq;
using WizardTea.Core;
using WizardTea.Blocks.NiMain;
using WizardTea.Core.Types;
using Avalonia;

namespace Imview.Core.Services;

public class NifGeometryProcessor
{
    // Cache for processed NIF files to avoid re-processing
    private static readonly Dictionary<string, ProcessedNifGeometry> _nifCache = new();
    private static readonly object _cacheLock = new();
    public class NifMeshData
    {
        public string Name { get; set; } = string.Empty;
        public List<Point> Vertices2D { get; set; } = new();
        public List<int[]> Triangles { get; set; } = new();
        public List<Point> BoundingBox { get; set; } = new();
        public bool HasValidGeometry => Vertices2D.Count > 0;
    }

    public class ProcessedNifGeometry
    {
        public List<NifMeshData> Meshes { get; set; } = new();
        public Point SceneBounds { get; set; }
        public bool HasGeometry => Meshes.Any(m => m.HasValidGeometry);
    }

    private const double CanvasCenterX = 10000.0;
    private const double CanvasCenterY = 10000.0;
    private const double ScaleFactor = 0.25; // Same scale factor used for zone objects

    public ProcessedNifGeometry ProcessNifFile(NifFile nifFile)
    {
        if (nifFile?.Blocks == null)
        {
            Console.WriteLine("[NIF] NIF file or blocks are null");
            return new ProcessedNifGeometry();
        }

        // Create a cache key based on NIF file content hash
        var cacheKey = GenerateCacheKey(nifFile);
        
        // Check cache first
        lock (_cacheLock)
        {
            if (_nifCache.TryGetValue(cacheKey, out var cachedResult))
            {
                Console.WriteLine($"[NIF] Using cached NIF geometry with {cachedResult.Meshes.Count} meshes");
                return cachedResult;
            }
        }

        var result = new ProcessedNifGeometry();
        Console.WriteLine($"[NIF] Processing NIF file with {nifFile.Blocks.Length} blocks");

        // First, let's examine what types of blocks we have
        var blockTypes = nifFile.Blocks.GroupBy(b => b.GetType().Name)
                                      .ToDictionary(g => g.Key, g => g.Count());
        
        Console.WriteLine($"[NIF] Block types found:");
        foreach (var kvp in blockTypes)
        {
            Console.WriteLine($"[NIF]   {kvp.Key}: {kvp.Value}");
        }

        // Find all geometry blocks first
        var geometryBlocks = nifFile.Blocks.OfType<NiTriBasedGeom>().ToList();
        Console.WriteLine($"[NIF] Found {geometryBlocks.Count} geometry blocks (NiTriBasedGeom)");

        // Find all geometry data blocks 
        var geometryDataBlocks = nifFile.Blocks.OfType<NiTriStripsData>().ToList();
        Console.WriteLine($"[NIF] Found {geometryDataBlocks.Count} geometry data blocks (NiTriStripsData)");

        // Try the newer approach: process geometry blocks and resolve their data references
        foreach (var geometry in geometryBlocks)
        {
            try
            {
                Console.WriteLine($"[NIF] Processing geometry block: {geometry.GetType().Name} - '{geometry.Name}'");
                
                var meshData = ProcessGeometry(geometry, nifFile);
                if (meshData.HasValidGeometry)
                {
                    result.Meshes.Add(meshData);
                    Console.WriteLine($"[NIF] ✓ Processed mesh '{meshData.Name}' with {meshData.Vertices2D.Count} vertices and {meshData.Triangles.Count} triangles");
                }
                else
                {
                    Console.WriteLine($"[NIF] ✗ Mesh '{meshData.Name}' has no valid geometry");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NIF] ✗ Error processing geometry block '{geometry.Name}': {ex.Message}");
            }
        }

        // Fallback: process data blocks directly if no geometry was found
        if (result.Meshes.Count == 0)
        {
            Console.WriteLine($"[NIF] No meshes from geometry blocks, trying data blocks directly...");
            
            foreach (var geometryData in geometryDataBlocks)
            {
                try
                {
                    var meshData = ProcessGeometryData(geometryData);
                    if (meshData.HasValidGeometry)
                    {
                        result.Meshes.Add(meshData);
                        Console.WriteLine($"[NIF] ✓ Processed data mesh '{meshData.Name}' with {meshData.Vertices2D.Count} vertices");
                    }
                    else
                    {
                        Console.WriteLine($"[NIF] ✗ Data mesh '{meshData.Name}' has no valid geometry");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NIF] ✗ Error processing geometry data block: {ex.Message}");
                }
            }
        }

        // Calculate overall scene bounds
        if (result.Meshes.Count > 0)
        {
            var allVertices = result.Meshes.SelectMany(m => m.Vertices2D).ToList();
            if (allVertices.Count > 0)
            {
                var minX = allVertices.Min(v => v.X);
                var maxX = allVertices.Max(v => v.X);
                var minY = allVertices.Min(v => v.Y);
                var maxY = allVertices.Max(v => v.Y);
                result.SceneBounds = new Point(maxX - minX, maxY - minY);
            }
        }

        Console.WriteLine($"[NIF] Processing complete: {result.Meshes.Count} meshes, scene bounds: {result.SceneBounds}");
        
        // Cache the result
        lock (_cacheLock)
        {
            _nifCache[cacheKey] = result;
            
            // Limit cache size to prevent memory issues
            if (_nifCache.Count > 10)
            {
                var oldestKey = _nifCache.Keys.First();
                _nifCache.Remove(oldestKey);
            }
        }
        
        return result;
    }

    private NifMeshData ProcessGeometryData(NiTriStripsData geometryData)
    {
        var meshData = new NifMeshData
        {
            Name = $"TriStripsData_{geometryData.GetHashCode()}"
        };

        // Extract vertices and convert to 2D
        var vertices3D = ExtractVertices(geometryData);
        if (vertices3D.Count == 0)
        {
            Console.WriteLine($"No vertices found for '{meshData.Name}'");
            return meshData;
        }

        Console.WriteLine($"Extracted {vertices3D.Count} vertices from '{meshData.Name}'");

        // Project 3D vertices to 2D canvas coordinates
        meshData.Vertices2D = vertices3D.Select(ProjectTo2D).ToList();

        // Extract triangle strips and convert to triangles
        meshData.Triangles = ExtractTriangleStripsFromData(geometryData);
        
        // Optimize mesh for large geometry
        if (meshData.Vertices2D.Count > 200)
        {
            // For very large meshes, create a simplified representation
            meshData.Vertices2D = SimplifyMesh(meshData.Vertices2D, 100);
            meshData.Triangles.Clear(); // Clear triangles for simplified mesh to avoid index issues
            Console.WriteLine($"Simplified large mesh '{meshData.Name}' from original vertex count to {meshData.Vertices2D.Count} vertices");
        }

        // Calculate 2D bounding box
        if (meshData.Vertices2D.Count > 0)
        {
            var minX = meshData.Vertices2D.Min(v => v.X);
            var maxX = meshData.Vertices2D.Max(v => v.X);
            var minY = meshData.Vertices2D.Min(v => v.Y);
            var maxY = meshData.Vertices2D.Max(v => v.Y);
            
            meshData.BoundingBox = new List<Point>
            {
                new Point(minX, minY),
                new Point(maxX, minY),
                new Point(maxX, maxY),
                new Point(minX, maxY)
            };
        }

        return meshData;
    }

    private NifMeshData ProcessGeometry(NiTriBasedGeom geometry, NifFile nifFile)
    {
        var meshData = new NifMeshData
        {
            Name = geometry.Name ?? "Unnamed Geometry"
        };

        Console.WriteLine($"[NIF] ProcessGeometry: '{meshData.Name}' - Data ref value: {geometry.Data?.Value}");

        // Get the geometry data by resolving the reference
        var data = ResolveGeometryData(geometry, nifFile);
        if (data == null)
        {
            Console.WriteLine($"[NIF] ✗ No geometry data found for '{meshData.Name}' (ref value: {geometry.Data?.Value})");
            return meshData;
        }

        Console.WriteLine($"[NIF] ✓ Found geometry data of type: {data.GetType().Name}");

        // Extract vertices and convert to 2D
        var vertices3D = ExtractVertices(data);
        if (vertices3D.Count == 0)
        {
            Console.WriteLine($"[NIF] ✗ No vertices found for '{meshData.Name}' (HasVertices: {data.HasVertices}, NumVertices: {data.NumVertices})");
            return meshData;
        }

        Console.WriteLine($"[NIF] ✓ Extracted {vertices3D.Count} vertices from '{meshData.Name}'");

        // Project 3D vertices to 2D canvas coordinates
        meshData.Vertices2D = vertices3D.Select(ProjectTo2D).ToList();

        // Extract triangle indices if available
        if (data is NiTriStripsData triStripsData)
        {
            meshData.Triangles = ExtractTriangleStripsFromData(triStripsData);
            Console.WriteLine($"[NIF] ✓ Extracted {meshData.Triangles.Count} triangles from triangle strips");
        }

        // Calculate 2D bounding box
        if (meshData.Vertices2D.Count > 0)
        {
            var minX = meshData.Vertices2D.Min(v => v.X);
            var maxX = meshData.Vertices2D.Max(v => v.X);
            var minY = meshData.Vertices2D.Min(v => v.Y);
            var maxY = meshData.Vertices2D.Max(v => v.Y);
            
            meshData.BoundingBox = new List<Point>
            {
                new Point(minX, minY),
                new Point(maxX, minY),
                new Point(maxX, maxY),
                new Point(minX, maxY)
            };
        }

        return meshData;
    }

    private NifMeshData ProcessGeometry(NiTriBasedGeom geometry)
    {
        // Legacy method for backwards compatibility
        var meshData = new NifMeshData
        {
            Name = geometry.Name ?? "Unnamed Geometry"
        };
        Console.WriteLine($"[NIF] Legacy ProcessGeometry called for '{meshData.Name}' - cannot resolve data without NifFile reference");
        return meshData;
    }

    private NiGeometryData? ResolveGeometryData(NiTriBasedGeom geometry, NifFile nifFile)
    {
        if (geometry.Data == null)
        {
            Console.WriteLine($"[NIF] Geometry '{geometry.Name}' has no data reference");
            return null;
        }

        if (!geometry.Data.HasReference)
        {
            Console.WriteLine($"[NIF] Geometry '{geometry.Name}' has no valid data reference (value: {geometry.Data.Value})");
            return null;
        }

        try
        {
            var geometryData = geometry.Data.GetReference(nifFile);
            if (geometryData != null)
            {
                Console.WriteLine($"[NIF] ✓ Resolved data reference to: {geometryData.GetType().Name}");
                return geometryData;
            }
            else
            {
                Console.WriteLine($"[NIF] ✗ Data reference returned null (value: {geometry.Data.Value})");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NIF] ✗ Error resolving data reference: {ex.Message}");
            return null;
        }
    }

    private NiGeometryData? GetGeometryData(NiTriBasedGeom geometry)
    {
        // Legacy method - kept for backwards compatibility
        Console.WriteLine($"[NIF] Legacy GetGeometryData called - this won't work without NifFile reference");
        return null;
    }

    private List<Vector3> ExtractVertices(NiGeometryData data)
    {
        var vertices = new List<Vector3>();
        
        if (data.HasVertices && data.Vertices != null)
        {
            foreach (var vertex in data.Vertices)
            {
                vertices.Add(vertex);
            }
        }
        
        return vertices;
    }

    private List<Vector3> ExtractVertices(NiTriStripsData data)
    {
        var vertices = new List<Vector3>();
        
        Console.WriteLine($"[NIF] ExtractVertices (NiTriStripsData): HasVertices={data.HasVertices}, NumVertices={data.NumVertices}");
        
        if (data.HasVertices && data.Vertices != null)
        {
            foreach (var vertex in data.Vertices)
            {
                vertices.Add(vertex);
            }
            Console.WriteLine($"[NIF] Successfully extracted {vertices.Count} vertices from NiTriStripsData");
        }
        else
        {
            Console.WriteLine($"[NIF] No vertices to extract: HasVertices={data.HasVertices}, Vertices is null={data.Vertices == null}");
        }
        
        return vertices;
    }

    private Point ProjectTo2D(Vector3 vertex3D)
    {
        // Convert 3D world coordinates to 2D canvas coordinates
        // Using orthographic projection (top-down view)
        
        var canvasX = CanvasCenterX + (vertex3D.X * ScaleFactor);
        var canvasY = CanvasCenterY - (vertex3D.Y * ScaleFactor); // Flip Y for canvas coordinate system
        
        return new Point(canvasX, canvasY);
    }

    private List<int[]> ExtractTriangleStrips(NiTriStrips triStrips, int vertexCount)
    {
        var triangles = new List<int[]>();
        
        // Extract triangle strip data and convert to individual triangles
        // This would need access to the NiTriStripsData to get the actual strip indices
        // For now, return empty list as placeholder
        
        return triangles;
    }

    private List<int[]> ExtractTriangleStripsFromData(NiTriStripsData data)
    {
        var triangles = new List<int[]>();
        
        if (data.HasPoints && data.Points != null && data.StripLengths != null)
        {
            int pointIndex = 0;
            
            // Process each strip
            for (int stripIndex = 0; stripIndex < data.NumStrips; stripIndex++)
            {
                var stripLength = data.StripLengths[stripIndex];
                
                if (stripLength >= 3)
                {
                    // Convert triangle strip to individual triangles
                    for (int i = 0; i < stripLength - 2; i++)
                    {
                        if (pointIndex + i + 2 < data.Points.Length)
                        {
                            int v0 = data.Points[pointIndex + i];
                            int v1 = data.Points[pointIndex + i + 1];
                            int v2 = data.Points[pointIndex + i + 2];
                            
                            // For triangle strips, every other triangle needs to be flipped to maintain winding order
                            if (i % 2 == 0)
                            {
                                triangles.Add(new[] { v0, v1, v2 });
                            }
                            else
                            {
                                triangles.Add(new[] { v0, v2, v1 });
                            }
                        }
                    }
                }
                
                pointIndex += stripLength;
            }
        }
        
        return triangles;
    }

    public static List<Point> SimplifyMesh(List<Point> vertices, int maxVertices = 100)
    {
        if (vertices.Count <= maxVertices)
            return vertices;

        // Simple vertex reduction - keep every nth vertex
        var step = vertices.Count / maxVertices;
        var simplified = new List<Point>();
        
        for (int i = 0; i < vertices.Count; i += step)
        {
            simplified.Add(vertices[i]);
        }

        return simplified;
    }

    public static List<Point> CalculateConvexHull(List<Point> points)
    {
        if (points.Count < 3)
            return points;

        // Simple convex hull calculation for visualization
        // Sort points by X coordinate
        var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        
        // Build lower hull
        var lower = new List<Point>();
        foreach (var point in sorted)
        {
            while (lower.Count >= 2 && CrossProduct(lower[^2], lower[^1], point) <= 0)
            {
                lower.RemoveAt(lower.Count - 1);
            }
            lower.Add(point);
        }

        // Build upper hull
        var upper = new List<Point>();
        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            var point = sorted[i];
            while (upper.Count >= 2 && CrossProduct(upper[^2], upper[^1], point) <= 0)
            {
                upper.RemoveAt(upper.Count - 1);
            }
            upper.Add(point);
        }

        // Remove the last point of each half because it's repeated
        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);

        return lower.Concat(upper).ToList();
    }

    private static double CrossProduct(Point o, Point a, Point b)
    {
        return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
    }

    private string GenerateCacheKey(NifFile nifFile)
    {
        // Generate a simple cache key based on block count and first few block types
        // This is a simplified approach - a real implementation might use actual file hash
        var blockTypes = nifFile.Blocks.Take(5).Select(b => b.GetType().Name);
        return $"NIF_{nifFile.Blocks.Length}_{string.Join("_", blockTypes)}";
    }

    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _nifCache.Clear();
            Console.WriteLine("NIF geometry cache cleared");
        }
    }

    public static int GetCacheSize()
    {
        lock (_cacheLock)
        {
            return _nifCache.Count;
        }
    }
}