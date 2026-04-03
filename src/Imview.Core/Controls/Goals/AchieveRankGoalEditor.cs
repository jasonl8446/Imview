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

using Avalonia.Controls;
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.Controls.Base;
using Imview.Core.Common.Constants;

namespace Imview.Core.Controls.Editors.Goals;

/// <summary>
/// Editor for Achieve Rank goal templates.
/// </summary>
public class AchieveRankGoalEditor : GoalEditorWindowBase {
    
    private readonly NumericUpDown _rankBox;
    private AchieveRankGoalTemplate RankTemplate => (AchieveRankGoalTemplate) Template;

    public AchieveRankGoalEditor(AchieveRankGoalTemplate? template = null) 
        : base(template ?? new AchieveRankGoalTemplate(), "Edit Achieve Rank Goal") {
        _rankBox = new NumericUpDown {
            Minimum = byte.MinValue,
            Maximum = byte.MaxValue,
            Value = 1
        };

        InitializeEditor();
    }

    public void InitializeEditor() {
        var mainPanel = CreateBaseControlsPanel();

        var rankContent = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Required Rank:", _rankBox)
            }
        };

        mainPanel.Children.Add(CreateGroupBox("Rank Settings", rankContent));
        mainPanel.Children.Add(CreateActionButtons(Save, Cancel));
    }

    public Control GetTypeSpecificControls() 
        => _rankBox;

    public bool ValidateState() 
        => _rankBox.Value >= _rankBox.Minimum && _rankBox.Value <= _rankBox.Maximum;

    public void SaveTypeSpecificValues() {
        RankTemplate.m_rank = (int)(_rankBox.Value ?? 1);
        // Note: m_goalType is set by the base class SaveValues() from the ComboBox selection
    }

    protected override void Save() {
        if (!ValidateState()) {
            // TODO: Show validation error
            return;
        }

        SaveValues();
        SaveTypeSpecificValues();
        ResultSource.SetResult(Template);
        Close();
    }

}