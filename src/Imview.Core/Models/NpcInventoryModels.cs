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

namespace Imview.Core.Models;

/// <summary>
/// Represents NPC inventory data from the world database NpcInventory collection
/// </summary>
public class NpcInventory
{
    /// <summary>
    /// The template ID of the NPC
    /// </summary>
    public ulong TemplateID { get; set; }

    /// <summary>
    /// List of item template IDs in the NPC's inventory
    /// </summary>
    public List<ulong> Inventory { get; set; } = new();
}
