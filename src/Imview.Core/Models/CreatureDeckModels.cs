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
using System.Collections.ObjectModel;

namespace Imview.Core.Models;

/// <summary>
/// Represents creature spell deck data from the world database CreatureSpellbook collection
/// </summary>
public class CreatureDeck
{
    /// <summary>
    /// The deck name for the creature (from DeckBehaviorTemplate.m_defaultDeck)
    /// </summary>
    public string DeckName { get; set; } = string.Empty;

    /// <summary>
    /// List of spell template IDs in the creature's deck
    /// </summary>
    public List<uint> SpellTemplateIds { get; set; } = new();
}

/// <summary>
/// Represents a spell item in the creature's deck for UI binding
/// </summary>
public class CreatureDeckSpellItem
{
    /// <summary>
    /// The spell template ID
    /// </summary>
    public uint SpellTemplateId { get; set; }

    /// <summary>
    /// Constructor for creating a new spell item
    /// </summary>
    public CreatureDeckSpellItem(uint spellTemplateId)
    {
        SpellTemplateId = spellTemplateId;
    }
}