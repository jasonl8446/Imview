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
using System.Threading.Tasks;
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.Services;
using Imview.PacketReader;

namespace Imview.Core.Services;

/// <summary>
/// Service for reading and processing quest templates from packet captures.
/// </summary>
public static class QuestPacketReaderService {

    /// <summary>
    /// Reads quest templates from a packet capture file.
    /// </summary>
    /// <param name="filePath">Path to the packet capture file.</param>
    /// <returns>A collection of quest templates found in the packet capture.</returns>
    public static async Task<ObservableCollection<QuestTemplate>> ReadQuestsFromPacketCaptureAsync(string filePath) {
        try {
            if (string.IsNullOrEmpty(filePath)) {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");
            }

            // Set up template manifest loader delegate for actor template ID resolution
            QuestBuilder.TemplateManifestLoader = () => {
                try {
                    return RootWadService.Instance.GetFile("TemplateManifest.xml");
                }
                catch (Exception ex) {
                    Console.WriteLine($"Warning: Could not load template manifest from Root.wad: {ex.Message}");
                    return null;
                }
            };

            // Build quests from packet capture using QuestBuilder.
            var quests = await QuestBuilder.BuildQuestsFromPacketCaptureAsync(filePath);
            var questCollection = new ObservableCollection<QuestTemplate>(quests);
            
            MessageService.Info($"Successfully extracted {questCollection.Count} quests from packet capture.")
                .WithDuration(TimeSpan.FromSeconds(5))
                .Send();
                
            return questCollection;
        }
        catch (Exception ex) {
            MessageService.Error($"Failed to extract quests from packet capture: {ex.Message}")
                .WithDuration(TimeSpan.FromSeconds(10))
                .Send();
                
            return [];
        }
    }

}