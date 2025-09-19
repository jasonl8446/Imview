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
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Collections;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.IO;
using Imview.Core.Models;
using Imview.Core.Services;
using Imview.Core.Database;
using ReactiveUI;
using System.Reactive;

namespace Imview.Core.ViewModels;

/// <summary>
/// Database lookup status for ResTeleport results
/// </summary>
public enum TeleportDataStatus
{
    NotApplicable,  // Not a ResTeleport result
    Loading,        // Currently loading from database
    Found,          // Data found in database
    NotFound,       // No data found in database
    Error          // Error occurred during lookup
}

/// <summary>
/// Represents a property node in the dynamic Result viewer tree
/// </summary>
public class PropertyNode : ReactiveObject
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; }
    public ObservableCollection<PropertyNode> Children { get; set; } = new();
    public TeleportDataStatus TeleportStatus { get; set; } = TeleportDataStatus.NotApplicable;
    public bool CanEdit { get; set; } = false;
}

/// <summary>
/// ViewModel for dynamically viewing Result object properties using reflection
/// </summary>
public class ResultDynamicViewerViewModel : ViewModelBase
{
    private Result? _result;
    private ObservableCollection<PropertyNode> _propertyNodes = new();
    private string? _zoneName;
    private string? _triggerName;
    private TeleportDataStatus _teleportDataStatus = TeleportDataStatus.NotApplicable;
    private bool _isReplacingWithDatabaseResult = false;

    public Result? Result
    {
        get => _result;
        set
        {
            this.RaiseAndSetIfChanged(ref _result, value);
            UpdatePropertyNodes();
        }
    }

    public ObservableCollection<PropertyNode> PropertyNodes
    {
        get => _propertyNodes;
        set => this.RaiseAndSetIfChanged(ref _propertyNodes, value);
    }

    /// <summary>
    /// Zone name for database lookup context
    /// </summary>
    public string? ZoneName
    {
        get => _zoneName;
        set 
        {
            this.RaiseAndSetIfChanged(ref _zoneName, value);
            // Trigger database lookup when zone name changes
            TryTeleportLookup();
        }
    }

    /// <summary>
    /// Trigger name for database lookup context
    /// </summary>
    public string? TriggerName
    {
        get => _triggerName;
        set 
        {
            this.RaiseAndSetIfChanged(ref _triggerName, value);
            // Trigger database lookup when trigger name changes
            TryTeleportLookup();
        }
    }

    /// <summary>
    /// Status of teleport data lookup from database
    /// </summary>
    public TeleportDataStatus TeleportDataStatus
    {
        get => _teleportDataStatus;
        set => this.RaiseAndSetIfChanged(ref _teleportDataStatus, value);
    }
    
    /// <summary>
    /// Command to edit teleport data in database
    /// </summary>
    public ReactiveCommand<PropertyNode, Unit> EditTeleportCommand { get; }
    
    public ResultDynamicViewerViewModel()
    {
        EditTeleportCommand = ReactiveCommand.Create<PropertyNode>(EditTeleport);
    }

    private void UpdatePropertyNodes()
    {
        PropertyNodes.Clear();
        
        if (Result == null)
            return;

        var resultType = Result.GetType();
        
        var resultNode = new PropertyNode
        {
            Name = "Result",
            Value = resultType.Name,
            TypeName = resultType.Name,
            HasChildren = true,
            IsExpanded = true
        };

        // Check if this is a ResTeleport result
        if (Result is ResTeleport)
        {
            // If we're replacing with database result, maintain Found status
            if (_isReplacingWithDatabaseResult)
            {
                resultNode.TeleportStatus = TeleportDataStatus.Found;
            }
            // Set initial status based on current data availability for original results
            else if (!string.IsNullOrEmpty(ZoneName) && !string.IsNullOrEmpty(TriggerName))
            {
                resultNode.TeleportStatus = TeleportDataStatus.Loading;
                TeleportDataStatus = TeleportDataStatus.Loading;
                // Try initial lookup
                TryTeleportLookup();
            }
            else
            {
                resultNode.TeleportStatus = TeleportDataStatus.NotApplicable;
                TeleportDataStatus = TeleportDataStatus.NotApplicable;
                // Try initial lookup in case data comes later
                TryTeleportLookup();
            }
        }

        PopulatePropertyNodes(Result, resultNode);
        PropertyNodes.Add(resultNode);
    }
    
    /// <summary>
    /// Attempts to perform teleport database lookup if all conditions are met
    /// </summary>
    private void TryTeleportLookup()
    {
        // Only proceed if we have a ResTeleport result and valid context
        if (Result is not ResTeleport || PropertyNodes.Count == 0)
            return;
            
        var resultNode = PropertyNodes.FirstOrDefault();
        if (resultNode == null)
            return;
            
        if (!string.IsNullOrEmpty(ZoneName) && !string.IsNullOrEmpty(TriggerName))
        {
            resultNode.TeleportStatus = TeleportDataStatus.Loading;
            TeleportDataStatus = TeleportDataStatus.Loading;
            _ = Task.Run(async () => await LoadTeleportDataAsync(resultNode));
        }
        else
        {
            if (resultNode.TeleportStatus != TeleportDataStatus.NotApplicable)
            {
                resultNode.TeleportStatus = TeleportDataStatus.NotFound;
                TeleportDataStatus = TeleportDataStatus.NotFound;
            }
        }
    }

    /// <summary>
    /// Loads teleport data from database for ResTeleport results
    /// </summary>
    private async Task LoadTeleportDataAsync(PropertyNode resultNode)
    {
        try
        {
            if (string.IsNullOrEmpty(ZoneName) || string.IsNullOrEmpty(TriggerName))
            {
                resultNode.TeleportStatus = TeleportDataStatus.NotApplicable;
                TeleportDataStatus = TeleportDataStatus.NotApplicable;
                return;
            }

            var teleportData = await ZoneTransferService.GetTeleportDataAsync(ZoneName, TriggerName);
            
            if (teleportData != null)
            {
                resultNode.TeleportStatus = TeleportDataStatus.Found;
                TeleportDataStatus = TeleportDataStatus.Found;
                
                // Replace the current Result with the database version
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _isReplacingWithDatabaseResult = true;
                    Result = teleportData;
                    // Ensure status remains as Found after replacement
                    TeleportDataStatus = TeleportDataStatus.Found;
                    // Enable editing for database-loaded results
                    if (PropertyNodes.Count > 0)
                    {
                        PropertyNodes[0].CanEdit = true;
                    }
                    _isReplacingWithDatabaseResult = false;
                    this.RaisePropertyChanged(nameof(TeleportDataStatus));
                });
            }
            else
            {
                resultNode.TeleportStatus = TeleportDataStatus.NotFound;
                TeleportDataStatus = TeleportDataStatus.NotFound;
            }
        }
        catch (Exception ex)
        {
            resultNode.TeleportStatus = TeleportDataStatus.Error;
            TeleportDataStatus = TeleportDataStatus.Error;
        }
    }

    /// <summary>
    /// Opens the ResTeleport editor to edit database teleport data
    /// </summary>
    private async void EditTeleport(PropertyNode node)
    {
        try
        {
            if (Result is not ResTeleport resTeleport)
            {
                MessageService.Error("Can only edit ResTeleport results.").Send();
                return;
            }
            
            if (string.IsNullOrEmpty(ZoneName) || string.IsNullOrEmpty(TriggerName))
            {
                MessageService.Error("Zone and trigger context required for database editing.").Send();
                return;
            }
            
            // Check database connection
            var store = WorldDatabase.Instance.Store;
            if (store == null)
            {
                MessageService.Error("Database connection required. Please ensure your certificate is configured and the database is accessible.").Send();
                return;
            }
            
            // Open the ResTeleport editor with database save functionality  
            var zoneDataService = new ZoneDataService();
            var editor = new Controls.Results.DatabaseResTeleportEditor(resTeleport, ZoneName, TriggerName, zoneDataService);
            
            // Find the main window as owner
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
                
            if (mainWindow != null)
            {
                await editor.ShowDialog(mainWindow);
            }
            else
            {
                // Fallback - show as regular window if no main window available
                editor.Show();
            }
            
            // Refresh the display after editing
            if (editor.WasSaved)
            {
                MessageService.Info("Teleport data saved to database. Refreshing display...").Send();
                // Reload from database to show updated data
                _ = Task.Run(async () =>
                {
                    var updatedData = await ZoneTransferService.GetTeleportDataAsync(ZoneName, TriggerName);
                    if (updatedData != null)
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Result = updatedData;
                        });
                    }
                });
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to open teleport editor: {ex.Message}").Send();
        }
    }

    private void PopulatePropertyNodes(object? obj, PropertyNode parentNode)
    {
        if (obj == null)
            return;

        var objType = obj.GetType();
        var properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties.OrderBy(p => p.Name))
        {
            try
            {
                var value = property.GetValue(obj);
                var propertyNode = CreatePropertyNode(property, value);
                parentNode.Children.Add(propertyNode);
            }
            catch (Exception ex)
            {
                // Handle properties that can't be accessed
                var errorNode = new PropertyNode
                {
                    Name = property.Name,
                    Value = $"<Error: {ex.Message}>",
                    TypeName = property.PropertyType.Name,
                    HasChildren = false
                };
                parentNode.Children.Add(errorNode);
            }
        }

        // Also include fields for completeness (some Result properties might be fields)
        var fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields.OrderBy(f => f.Name))
        {
            try
            {
                var value = field.GetValue(obj);
                var fieldNode = CreateFieldNode(field, value);
                parentNode.Children.Add(fieldNode);
            }
            catch (Exception ex)
            {
                var errorNode = new PropertyNode
                {
                    Name = field.Name,
                    Value = $"<Error: {ex.Message}>",
                    TypeName = field.FieldType.Name,
                    HasChildren = false
                };
                parentNode.Children.Add(errorNode);
            }
        }
    }

    private PropertyNode CreatePropertyNode(PropertyInfo property, object? value)
    {
        var node = new PropertyNode
        {
            Name = property.Name,
            TypeName = property.PropertyType.Name
        };

        if (value == null)
        {
            node.Value = "<null>";
            node.HasChildren = false;
            return node;
        }

        var propertyType = property.PropertyType;

        // Handle primitive types and strings
        if (IsPrimitiveType(propertyType))
        {
            node.Value = FormatPrimitiveValue(value);
            node.HasChildren = false;
        }
        // Handle ByteString specially
        else if (propertyType == typeof(ByteString))
        {
            var byteString = (ByteString)value;
            node.Value = byteString.ToString();
            node.HasChildren = false;
        }
        // Handle collections
        else if (IsCollectionType(propertyType) && value is System.Collections.IEnumerable enumerable)
        {
            var itemCount = enumerable.Cast<object>().Count();
            node.Value = $"[{itemCount} items]";
            node.HasChildren = itemCount > 0;

            if (node.HasChildren)
            {
                PopulateCollectionNodes(enumerable, node);
            }
        }
        // Handle complex objects
        else
        {
            node.Value = $"<{propertyType.Name}>";
            node.HasChildren = true;
            PopulatePropertyNodes(value, node);
        }

        return node;
    }

    private PropertyNode CreateFieldNode(FieldInfo field, object? value)
    {
        var node = new PropertyNode
        {
            Name = field.Name,
            TypeName = field.FieldType.Name
        };

        if (value == null)
        {
            node.Value = "<null>";
            node.HasChildren = false;
            return node;
        }

        var fieldType = field.FieldType;

        // Handle primitive types and strings
        if (IsPrimitiveType(fieldType))
        {
            node.Value = FormatPrimitiveValue(value);
            node.HasChildren = false;
        }
        // Handle ByteString specially
        else if (fieldType == typeof(ByteString))
        {
            var byteString = (ByteString)value;
            node.Value = byteString.ToString();
            node.HasChildren = false;
        }
        // Handle collections
        else if (IsCollectionType(fieldType) && value is System.Collections.IEnumerable enumerable)
        {
            var itemCount = enumerable.Cast<object>().Count();
            node.Value = $"[{itemCount} items]";
            node.HasChildren = itemCount > 0;

            if (node.HasChildren)
            {
                PopulateCollectionNodes(enumerable, node);
            }
        }
        // Handle complex objects
        else
        {
            node.Value = $"<{fieldType.Name}>";
            node.HasChildren = true;
            PopulatePropertyNodes(value, node);
        }

        return node;
    }

    private void PopulateCollectionNodes(System.Collections.IEnumerable collection, PropertyNode parentNode)
    {
        int index = 0;
        foreach (var item in collection)
        {
            var itemNode = new PropertyNode
            {
                Name = $"[{index}]",
                TypeName = item?.GetType()?.Name ?? "null"
            };

            if (item == null)
            {
                itemNode.Value = "<null>";
                itemNode.HasChildren = false;
            }
            else if (IsPrimitiveType(item.GetType()))
            {
                itemNode.Value = FormatPrimitiveValue(item);
                itemNode.HasChildren = false;
            }
            else if (item is ByteString byteString)
            {
                itemNode.Value = byteString.ToString();
                itemNode.HasChildren = false;
            }
            else
            {
                itemNode.Value = $"<{item.GetType().Name}>";
                itemNode.HasChildren = true;
                PopulatePropertyNodes(item, itemNode);
            }

            parentNode.Children.Add(itemNode);
            index++;
        }
    }

    private bool IsPrimitiveType(System.Type type)
    {
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(decimal) || 
               type == typeof(DateTime) || 
               type == typeof(TimeSpan) ||
               type.IsEnum ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                Nullable.GetUnderlyingType(type)?.IsPrimitive == true);
    }

    private bool IsCollectionType(System.Type type)
    {
        if (type == typeof(string))
            return false;

        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    private string FormatPrimitiveValue(object value)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            string s => $"\"{s}\"",
            byte b => b.ToString(),
            sbyte sb => sb.ToString(),
            short s => s.ToString(),
            ushort us => us.ToString(),
            int i => i.ToString(),
            uint ui => ui.ToString(),
            long l => l.ToString(),
            ulong ul => ul.ToString(),
            float f => f.ToString("F6"),
            double d => d.ToString("F6"),
            decimal dec => dec.ToString(),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            TimeSpan ts => ts.ToString(),
            Enum e => $"{e} ({Convert.ToInt32(e)})",
            _ => value.ToString() ?? "<null>"
        };
    }
}
