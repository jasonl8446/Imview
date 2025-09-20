// Copyright (c) 2025 Jay Kulsh
// Licensed under the BSD 3-Clause License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imview.Core.Database.Models
{
    /// <summary>
    /// Represents the complete spell inventory for an NPC professor
    /// </summary>
    public class NpcSpellInventory
    {
        /// <summary>
        /// The NPC template ID that this spell inventory belongs to
        /// </summary>
        [Required]
        public ulong TemplateID { get; set; }
        
        /// <summary>
        /// List of spells that this NPC professor can teach
        /// </summary>
        [Required]
        public List<SpellInventoryItem> Spells { get; set; }

        /// <summary>
        /// Creates a new NPC spell inventory with the specified template ID
        /// </summary>
        /// <param name="templateId">The NPC template ID</param>
        public NpcSpellInventory(ulong templateId = 0)
        {
            TemplateID = templateId;
            Spells = new List<SpellInventoryItem>();
        }
    }
}
