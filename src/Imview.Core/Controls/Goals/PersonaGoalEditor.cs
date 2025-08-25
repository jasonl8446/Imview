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
using Imcodec.IO;
using Imcodec.ObjectProperty.TypeCache;
using Imview.Core.Common.Constants;
using Imview.Core.Controls.Base;
using Imview.Core.Controls.Templates;

namespace Imview.Core.Controls.Goals;

public class PersonaGoalEditor : GoalEditorWindowBase {

    private readonly TextBox _personaNameBox;
    private readonly CheckBox _usePatronBox;
    private readonly QuestDialogEditor _dialogEditor;
    private PersonaGoalTemplate PersonaTemplate => (PersonaGoalTemplate)Template;

    public PersonaGoalEditor(PersonaGoalTemplate? template = null)
        : base(template ?? new PersonaGoalTemplate(), "Edit Persona Goal") {
        _personaNameBox = new TextBox();
        _usePatronBox = new CheckBox { Content = "Use Patron" };
        _dialogEditor = new QuestDialogEditor();

        InitializeEditor();
    }

    public void InitializeEditor() {
        var mainPanel = CreateBaseControlsPanel();

        var personaContent = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Persona Name:", _personaNameBox),
                _usePatronBox
            }
        };

        mainPanel.Children.Add(CreateGroupBox("Persona Settings", personaContent));
        mainPanel.Children.Add(CreateGroupBox("Step-Specific Dialogue", _dialogEditor));
        mainPanel.Children.Add(CreateActionButtons(Save, Cancel));

        if (PersonaTemplate != null) {
            _personaNameBox.Text = PersonaTemplate.m_personaName?.ToString() ?? string.Empty;
            _usePatronBox.IsChecked = PersonaTemplate.m_usePatron;
            
            // Initialize dialog editor with goal's dialog list
            _dialogEditor.DialogList = PersonaTemplate.m_dialogList as ActorDialogList;
        }
    }

    public Control GetTypeSpecificControls() 
        => new StackPanel {
            Children = {
                    _personaNameBox,
                    _usePatronBox
                }
        };

    public bool ValidateState() 
        => !string.IsNullOrWhiteSpace(_personaNameBox.Text);

    public void SaveTypeSpecificValues() {
        PersonaTemplate.m_personaName = new ByteString(_personaNameBox.Text ?? string.Empty);
        PersonaTemplate.m_usePatron = _usePatronBox.IsChecked ?? false;
        PersonaTemplate.m_goalType = GOAL_TYPE.GOAL_TYPE_PERSONA;
        
        // Save dialog system - convert from editor back to ActorDialogList
        PersonaTemplate.m_dialogList = _dialogEditor.ToActorDialogList();
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
