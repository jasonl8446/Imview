// Copyright (c) 2025 Jay Kulsh
// Licensed under the BSD 3-Clause License. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Imview.Core.Database.Models
{
    /// <summary>
    /// Represents a single spell that an NPC professor can teach
    /// </summary>
    public class SpellInventoryItem
    {
        /// <summary>
        /// The template ID of the spell
        /// </summary>
        [Required]
        public ulong TemplateID { get; set; }
        
        /// <summary>
        /// The template ID of a spell that must be known before this spell can be learned.
        /// 0 means no prerequisite spell is required
        /// </summary>
        public ulong RequiredSpellID { get; set; }
        
        /// <summary>
        /// The minimum level required to learn this spell
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Level must be greater than 0")]
        public int Level { get; set; }

        /// <summary>
        /// Creates a new spell inventory item with the specified parameters
        /// </summary>
        /// <param name="templateId">The spell template ID</param>
        /// <param name="requiredSpellId">The required prerequisite spell ID (0 for none)</param>
        /// <param name="level">The minimum level required to learn the spell</param>
        public SpellInventoryItem(ulong templateId = 0, ulong requiredSpellId = 0, int level = 1)
        {
            TemplateID = templateId;
            RequiredSpellID = requiredSpellId;
            Level = level;
        }
    }
}
