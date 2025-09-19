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

using System.Collections.Generic;
using Imcodec.ObjectProperty.TypeCache;

namespace Imview.Core.Models;

/// <summary>
/// Represents zone data from the world database ZoneTransfer collection
/// </summary>
public class WizardZoneData
{
    /// <summary>
    /// The name of the zone
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// List of teleport data for triggers in this zone
    /// </summary>
    public List<WizardTeleportData> Teleports { get; set; } = new();
}

/// <summary>
/// Represents teleport data for a specific trigger
/// </summary>
public class WizardTeleportData
{
    /// <summary>
    /// Name of the trigger that activates this teleport
    /// </summary>
    public string TriggerName { get; set; } = string.Empty;

    /// <summary>
    /// The ResTeleport object
    /// </summary>
    public ResTeleport Teleport { get; set; } = new();
}

/// <summary>
/// Legacy zone transfer document - kept for backwards compatibility
/// </summary>
public class ZoneTransferDocument
{
    /// <summary>
    /// The name of the zone
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// List of teleport data for triggers in this zone
    /// </summary>
    public List<ZoneTeleportData> Teleports { get; set; } = new();
}

/// <summary>
/// Legacy teleport data for a specific trigger - kept for backwards compatibility
/// </summary>
public class ZoneTeleportData
{
    /// <summary>
    /// Name of the trigger that activates this teleport
    /// </summary>
    public string TriggerName { get; set; } = string.Empty;

    /// <summary>
    /// The teleport information
    /// </summary>
    public TeleportInfo Teleport { get; set; } = new();
}

/// <summary>
/// Legacy teleport information containing destination details - kept for backwards compatibility
/// </summary>
public class TeleportInfo
{
    /// <summary>
    /// Destination zone name
    /// </summary>
    public string DestinationZone { get; set; } = string.Empty;

    /// <summary>
    /// Destination coordinates
    /// </summary>
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    /// <summary>
    /// Destination rotation
    /// </summary>
    public float Yaw { get; set; }
}
