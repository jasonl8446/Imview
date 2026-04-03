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

using Avalonia;
using Avalonia.Controls;
using Imview.Core.Controls.Editors.Goals;
using System.Threading.Tasks;
using Imcodec.ObjectProperty.TypeCache;
using Avalonia.Controls.ApplicationLifetimes;
using Imview.Core.Controls.Base;
using System;
using Imview.Core.Models;

namespace Imview.Core.Controls.Goals;

/// <summary>
/// Factory for creating goal editors.
/// </summary>
public interface IGoalEditorFactory {

    Task<GoalTemplate> CreateEditor(GoalTemplate template, string? questTitle = null);

}

public class GoalEditorFactory : IGoalEditorFactory {

    public async Task<GoalTemplate> CreateEditor(GoalTemplate template, string? questTitle = null) {
        Avalonia.Controls.Window editor = template switch {
            AchieveRankGoalTemplate rankTemplate => new AchieveRankGoalEditor(rankTemplate),
            BountyGoalTemplate bountyTemplate => new BountyGoalEditor(bountyTemplate),
            PersonaGoalTemplate personaTemplate => new PersonaGoalEditor(personaTemplate),
            ScavengeGoalTemplate scavengeTemplate => new ScavengeGoalEditor(scavengeTemplate),
            UsageGoalTemplate usageTemplate => new UsageGoalEditor(usageTemplate),
            WaypointGoalTemplate waypointTemplate => new WaypointGoalEditor(waypointTemplate),
            _ => throw new ArgumentException($"Unsupported goal type: {template?.GetType().Name ?? "null"}")
        };

        // Set quest title on the tally counter editor if available
        if (editor is GoalEditorWindowBase goalEditor)
        {
            goalEditor.TallyCounterEditor.QuestTitle = questTitle;
        }

        var editorWindow = (EditorWindowBase<GoalTemplate>)editor;
        var appLifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
            ?? throw new InvalidOperationException("Application lifetime is not set or is not of the expected type.");
        var mainWindow = appLifetime.MainWindow 
            ?? throw new InvalidOperationException("Main window is not set.");
        await editor.ShowDialog(mainWindow);
        
        return await editorWindow.GetResultAsync();
    }
    
}