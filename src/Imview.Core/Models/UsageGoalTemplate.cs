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
/// Represents a usage goal template, similar to scavenge goals.
/// Usage goals track item usage with adjectives and totals.
/// </summary>
public record UsageGoalTemplate : GoalTemplate
{
    /// <summary>
    /// List of item adjectives that describe the items to be used.
    /// </summary>
    public List<string> m_itemAdjectives { get; set; } = new();

    /// <summary>
    /// The total number of items to use.
    /// </summary>
    public int m_itemTotal { get; set; }
}