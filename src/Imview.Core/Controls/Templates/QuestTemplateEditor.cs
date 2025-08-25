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

// QuestTemplateEditor.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ReactiveUI;
using System.Collections.ObjectModel;
using Imcodec.ObjectProperty.TypeCache;
using System;
using System.Collections.Generic;
using Imview.Core.Common.Constants;
using System.Linq;
using Imcodec.IO;
using Imview.Core.Controls.Goals;
using Avalonia.Controls.Templates;
using System.Threading.Tasks;
using Imview.Core.Services;
using Avalonia.VisualTree;
using Avalonia.ReactiveUI;

namespace Imview.Core.Controls.Templates;

/// <summary>
/// Editor for Quest templates that manages both basic quest properties and associated goals.
/// </summary>
public partial class QuestTemplateEditor : UserControl {

    public static new readonly StyledProperty<QuestTemplate> TemplateProperty =
        AvaloniaProperty.Register<QuestTemplateEditor, QuestTemplate>(nameof(Template));

    public new QuestTemplate Template {
        get => GetValue(TemplateProperty);
        set {
            SetValue(TemplateProperty, value);
            if (value != null) {
                PopulateFieldsWithTemplate(value);
            }
        }
    }

    // Core properties
    private QuestTemplate _template;
    private readonly ObservableCollection<GoalTemplateWrapper> _goals;
    private readonly ObservableCollection<GoalCompleteLogicWrapper> _goalLogics;
    private readonly IGoalEditorFactory _goalEditorFactory;

    // UI Controls
    private TextBox _questNameBox;
    private TextBox _questTitleBox;
    private NumericUpDown _questLevelBox;
    private ListBox _goalsList;
    private ListBox _goalLogicsList;
    private QuestDialogEditor _questDialogEditor;
    private TextBox _onStartScriptBox;
    private TextBox _onEndScriptBox;

    // Checkboxes
    private CheckBox _isHiddenBox;
    private CheckBox _noQuestHelperBox;
    private CheckBox _prepAlwaysBox;
    private CheckBox _questRepeatBox;
    private CheckBox _outdatedBox;

    // Parameter-less constructor for design-time support
    public QuestTemplateEditor() : this(null, null) { }

    public QuestTemplateEditor(QuestTemplate? template = null, IGoalEditorFactory? goalEditorFactory = null) {
        _template = template ?? new QuestTemplate();
        _goalEditorFactory = goalEditorFactory ?? new GoalEditorFactory();

        // Create goal wrappers with IsStartGoal property,
        _goals = new ObservableCollection<GoalTemplateWrapper>(
            (_template.m_goals ?? []).Select(g => {
                var isStart = _template.m_startGoals?.Any(sg => sg.ToString() == g.m_goalName?.ToString()) ?? false;
                return new GoalTemplateWrapper(g, isStart);
            })
        );

        // Initialize goal logic wrappers,
        _goalLogics = new ObservableCollection<GoalCompleteLogicWrapper>(
            (_template.m_goalLogic ?? []).Select(logic => new GoalCompleteLogicWrapper(logic))
        );

        // Initialize non-nullable fields
        _questNameBox = new TextBox();
        _questTitleBox = new TextBox();
        _questLevelBox = new NumericUpDown();
        _goalsList = new ListBox();
        _goalLogicsList = new ListBox();
        _questDialogEditor = new QuestDialogEditor();
        _onStartScriptBox = new TextBox();
        _onEndScriptBox = new TextBox();
        _isHiddenBox = new CheckBox();
        _noQuestHelperBox = new CheckBox();
        _prepAlwaysBox = new CheckBox();
        _questRepeatBox = new CheckBox();
        _outdatedBox = new CheckBox();

        InitializeComponent();
        InitializeValues();
    }

    private void InitializeComponent() {
        var mainPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Margin = EditorConstants.DEFAULT_MARGIN_THICKNESS
        };

        // Initialize controls, then build the UI.
        InitializeControls();

        mainPanel.Children.Add(CreateBasicInfoSection());
        mainPanel.Children.Add(CreateDialogSection());
        mainPanel.Children.Add(CreateScriptSection());
        mainPanel.Children.Add(CreateFlagsSection());
        mainPanel.Children.Add(CreateGoalsSection());
        mainPanel.Children.Add(CreateGoalLogicSection());
        mainPanel.Children.Add(CreateActionButtons());

        Content = new ScrollViewer { Content = mainPanel };
    }

    private void InitializeControls() {
        // Basic info controls.
        _questNameBox = new TextBox();
        _questTitleBox = new TextBox();
        _questLevelBox = new NumericUpDown {
            Minimum = 1,
            Maximum = 200,
            Value = 1
        };

        // Dialog editor is initialized above

        // Script controls. These are the names of scripts that 
        // will be executed when the quest starts or ends.
        _onStartScriptBox = new TextBox { AcceptsReturn = true, Height = 60 };
        _onEndScriptBox = new TextBox { AcceptsReturn = true, Height = 60 };

        // Quest flags.
        _isHiddenBox = new CheckBox { Content = "Is Hidden" };
        _noQuestHelperBox = new CheckBox { Content = "No Quest Helper" };
        _prepAlwaysBox = new CheckBox { Content = "Prep Always" };
        _questRepeatBox = new CheckBox { Content = "Quest Repeatable" };
        _outdatedBox = new CheckBox { Content = "Outdated" };

        // Goals list.
        _goalsList = new ListBox {
            ItemsSource = _goals,
            Height = 200
        };

        // Create a DataTemplate for the goal items
        _goalsList.ItemTemplate = new FuncDataTemplate<GoalTemplateWrapper>((goal, _) => {
            if (goal == null) {
                return null;
            }

            var panel = new DockPanel();

            // CheckBox for marking a goal as a starting goal
            var startGoalCheck = new CheckBox {
                Content = "Start Goal",
                IsChecked = goal.IsStartGoal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            startGoalCheck.IsCheckedChanged += (s, e) => {
                goal.IsStartGoal = startGoalCheck.IsChecked ?? false;
            };

            // Goal name text display
            var goalText = new TextBlock {
                Text = goal.Goal.m_goalName?.ToString() ?? $"Goal ({goal.Goal.m_goalType})",
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(startGoalCheck);
            panel.Children.Add(goalText);
            DockPanel.SetDock(startGoalCheck, Dock.Left);

            return panel;
        });

        _goalsList.DoubleTapped += GoalsList_DoubleTapped;

        // Goal Logic list
        _goalLogicsList = new ListBox {
            ItemsSource = _goalLogics,
            Height = 200,

            // Create a DataTemplate for the goal logic items
            ItemTemplate = new FuncDataTemplate<GoalCompleteLogicWrapper>((logicWrapper, _) => {
                if (logicWrapper == null) {
                    return null;
                }

                var logic = logicWrapper.Logic;
                var panel = new StackPanel { Spacing = 5 };

                // AND Goals
                if (logic.m_goalsAND?.Count > 0) {
                    panel.Children.Add(new TextBlock {
                        Text = $"AND Goals: {string.Join(", ", logic.m_goalsAND.Select(g => g.ToString()))}",
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                // OR Goals
                if (logic.m_goalsOR?.Count > 0) {
                    panel.Children.Add(new TextBlock {
                        Text = $"OR Goals ({logic.m_requiredORCount} required): {string.Join(", ", logic.m_goalsOR.Select(g => g.ToString()))}",
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                // Goals to Add
                if (logic.m_goalsToAdd?.Count > 0) {
                    panel.Children.Add(new TextBlock {
                        Text = $"Goals to Add: {string.Join(", ", logic.m_goalsToAdd.Select(g => g.ToString()))}",
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                // Complete Quest
                if (logic.m_completeQuest) {
                    panel.Children.Add(new TextBlock {
                        Text = "Completes Quest when satisfied",
                        FontStyle = FontStyle.Italic
                    });
                }

                return panel;
            })
        };

        _goalLogicsList.DoubleTapped += GoalLogicsList_DoubleTapped;
    }

    private Control CreateBasicInfoSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Quest Name:", _questNameBox),
                CreateLabeledControl("Quest Title:", _questTitleBox),
                CreateLabeledControl("Quest Level:", _questLevelBox)
            }
        };

        return CreateGroupBox("Basic Quest Information", content);
    }

    private Control CreateDialogSection() {
        return CreateGroupBox("Quest Dialogue System", _questDialogEditor);
    }

    private Control CreateScriptSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("On Start Script:", _onStartScriptBox),
                CreateLabeledControl("On End Script:", _onEndScriptBox)
            }
        };

        return CreateGroupBox("Quest Scripts", content);
    }

    private Control CreateFlagsSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                _isHiddenBox,
                _noQuestHelperBox,
                _prepAlwaysBox,
                _questRepeatBox,
                _outdatedBox
            }
        };

        return CreateGroupBox("Quest Flags", content);
    }

    private Control CreateGoalsSection() {
        var infoPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 5),
            Children = {
                new TextBlock {
                    Text = "Check 'Start Goal' for goals that should be activated when the quest begins.",
                    Foreground = Brushes.LightGray,
                    FontStyle = FontStyle.Italic
                }
            }
        };

        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateGoalButton("Add Bounty Goal", () => AddGoal<BountyGoalTemplate>()),
                CreateGoalButton("Add Persona Goal", () => AddGoal<PersonaGoalTemplate>()),
                CreateGoalButton("Add Scavenge Goal", () => AddGoal<ScavengeGoalTemplate>()),
                CreateGoalButton("Add Waypoint Goal", () => AddGoal<WaypointGoalTemplate>()),
                CreateGoalButton("Add Achieve Rank Goal", () => AddGoal<AchieveRankGoalTemplate>())
            }
        };

        var content = new DockPanel {
            LastChildFill = true,
            Children = {
                infoPanel,
                buttonPanel,
                _goalsList
            }
        };
        DockPanel.SetDock(infoPanel, Dock.Top);
        DockPanel.SetDock(buttonPanel, Dock.Top);

        return CreateGroupBox("Quest Goals", content);
    }

    private Control CreateGoalLogicSection() {
        var infoPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 5),
            Children = {
                new TextBlock {
                    Text = "Define how goals combine to progress the quest or complete it.",
                    Foreground = Brushes.LightGray,
                    FontStyle = FontStyle.Italic
                }
            }
        };

        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new Avalonia.Controls.Button {
                    Content = "Add Goal Logic",
                    Command = ReactiveCommand.Create(AddGoalLogic)
                },
                new Avalonia.Controls.Button {
                    Content = "Remove Selected",
                    Command = ReactiveCommand.Create(() => RemoveSelectedGoalLogic())
                }
            }
        };

        var content = new DockPanel {
            LastChildFill = true,
            Children = {
                infoPanel,
                buttonPanel,
                _goalLogicsList
            }
        };
        DockPanel.SetDock(infoPanel, Dock.Top);
        DockPanel.SetDock(buttonPanel, Dock.Top);

        return CreateGroupBox("Goal Completion Logic", content);
    }

    private Control CreateActionButtons()
        => new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = {
                new Avalonia.Controls.Button {
                    Content = "Save Template",
                    Command = ReactiveCommand.Create(SaveTemplate)
                },
                new Avalonia.Controls.Button {
                    Content = "Load Template",
                    Command = ReactiveCommand.Create(LoadTemplate)
                }
            }
        };

    private static Control CreateLabeledControl(string labelText, Control control)
        => new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { Text = labelText },
                control
            }
        };

    private static Border CreateGroupBox(string header, Control content)
        => new() {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = EditorConstants.DEFAULT_CORNER_RADIUS,
            Padding = EditorConstants.DEFAULT_GROUP_PADDING,
            Margin = new Thickness(0, 0, 0, 10),
            Child = new StackPanel {
                Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
                Children = {
                    new TextBlock {
                        Text = header,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 0, 0, 5)
                    },
                    content
                }
            }
        };

    private static Avalonia.Controls.Button CreateGoalButton(string content, System.Action onClick)
        => new() {
            Content = content,
            Command = ReactiveCommand.Create(onClick)
        };

    private void InitializeValues() {
        _questNameBox.Text = _template.m_questName?.ToString();
        _questTitleBox.Text = _template.m_questTitle?.ToString();
        _questLevelBox.Value = _template.m_questLevel;
        
        // Initialize dialog editor with quest's dialog list
        _questDialogEditor.DialogList = _template.m_dialogList as ActorDialogList;
        
        _onStartScriptBox.Text = _template.m_onStartQuestScript?.ToString();
        _onEndScriptBox.Text = _template.m_onEndQuestScript?.ToString();

        _isHiddenBox.IsChecked = _template.m_isHidden;
        _noQuestHelperBox.IsChecked = _template.m_noQuestHelper;
        _prepAlwaysBox.IsChecked = _template.m_prepAlways;
        _questRepeatBox.IsChecked = _template.m_questRepeat >= 1;
        _outdatedBox.IsChecked = _template.m_outdated;
    }

    private async void AddGoal<T>() where T : GoalTemplate, new() {
        var result = await _goalEditorFactory.CreateEditor(new T());
        if (result != null) {
            _goals.Add(new GoalTemplateWrapper(result, false));
        }
    }

    private async void GoalsList_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (_goalsList.SelectedItem is GoalTemplateWrapper selectedGoalWrapper) {
            var result = await _goalEditorFactory.CreateEditor(selectedGoalWrapper.Goal);
            if (result != null) {
                var index = _goals.IndexOf(selectedGoalWrapper);
                _goals[index] = new GoalTemplateWrapper(result, selectedGoalWrapper.IsStartGoal);
            }
        }
    }

    private async void GoalLogicsList_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (_goalLogicsList.SelectedItem is GoalCompleteLogicWrapper selectedLogicWrapper) {
            var result = await ShowGoalLogicEditor(selectedLogicWrapper.Logic);
            if (result != null) {
                var index = _goalLogics.IndexOf(selectedLogicWrapper);
                _goalLogics[index] = new GoalCompleteLogicWrapper(result);
            }
        }
    }

    private async Task<GoalCompleteLogic?> ShowGoalLogicEditor(GoalCompleteLogic? logicToEdit = null) {
        try {
            // Get all goal names for selection in the editor
            var goalNames = _goals
                .Select(wrapper => wrapper.Goal.m_goalName?.ToString() ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList()!;

            // Create and show the goal logic editor
            var editor = new GoalCompleteLogicEditor(logicToEdit, goalNames);

            // Show as a dialog
            var appLifetime = Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (appLifetime?.MainWindow != null) {
                await editor.ShowDialog(appLifetime.MainWindow);
                return await editor.GetResultAsync();
            }

            return null;
        }
        catch (Exception ex) {
            // TODO: Show error dialog
            Console.WriteLine($"Error showing goal logic editor: {ex}");
            return null;
        }
    }

    private async void AddGoalLogic() {
        var newLogic = await ShowGoalLogicEditor();
        if (newLogic is not null) {
            _goalLogics.Add(new GoalCompleteLogicWrapper((GoalCompleteLogic) newLogic));
        }
    }

    private void RemoveSelectedGoalLogic() {
        if (_goalLogicsList.SelectedItem is GoalCompleteLogicWrapper selectedLogicWrapper) {
            _goalLogics.Remove(selectedLogicWrapper);
        }
    }

    private async void SaveTemplate() {
        try {
            // Save basic quest properties.
            _template.m_questName = new ByteString(_questNameBox.Text ?? string.Empty);
            _template.m_questTitle = new ByteString(_questTitleBox.Text ?? string.Empty);
            _template.m_questLevel = (int) (_questLevelBox.Value ?? 1);
            
            // Save dialog system - convert from editor back to ActorDialogList
            _template.m_dialogList = _questDialogEditor.ToActorDialogList();
            
            _template.m_onStartQuestScript = new ByteString(_onStartScriptBox.Text ?? string.Empty);
            _template.m_onEndQuestScript = new ByteString(_onEndScriptBox.Text ?? string.Empty);

            // Save quest flags.
            _template.m_isHidden = _isHiddenBox.IsChecked ?? false;
            _template.m_noQuestHelper = _noQuestHelperBox.IsChecked ?? false;
            _template.m_prepAlways = _prepAlwaysBox.IsChecked ?? false;
            _template.m_questRepeat = (_questRepeatBox.IsChecked ?? false) ? 1 : 0;
            _template.m_outdated = _outdatedBox.IsChecked ?? false;

            // Save quest goals.
            _template.m_goals = _goals.Select(wrapper => wrapper.Goal).ToList();

            // Save starting goals.
            _template.m_startGoals = _goals
                .Where(wrapper => wrapper.IsStartGoal)
                .Select(wrapper => wrapper.Goal.m_goalName?.ToString() ?? string.Empty)
                .ToList();

            // Save goal logic.
            _template.m_goalLogic = _goalLogics.Select(wrapper => wrapper.Logic).ToList();

            // Get the parent window for the save dialog.
            var parentWindow = this.FindAncestorOfType<Avalonia.Controls.Window>();
            if (parentWindow == null) {
                var appLifetime = Application.Current?.ApplicationLifetime
                    as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                parentWindow = appLifetime?.MainWindow;
            }

            if (parentWindow == null) {
                MessageService
                    .Error("Could not find parent window for save dialog.")
                    .Send();
                return;
            }

            var success = await TemplateSerializer.SaveTemplateAsync(_template, parentWindow);
            if (success) {
                MessageService
                    .Info("Quest template saved successfully.")
                    .Send();
            }
        }
        catch (Exception ex) {
            MessageService
                .Error($"Error saving template: {ex.Message}")
                .Send();
        }
    }

    private async void LoadTemplate() {
        try {
            // Get the parent window for the load dialog
            var parentWindow = this.FindAncestorOfType<Avalonia.Controls.Window>();
            if (parentWindow == null) {
                var appLifetime = Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                parentWindow = appLifetime?.MainWindow;
            }

            if (parentWindow == null) {
                MessageService
                    .Error("Could not find parent window for load dialog.")
                    .Send();
                return;
            }

            var loadedTemplate = await TemplateSerializer.LoadTemplateAsync(parentWindow);

            if (loadedTemplate != null) {
                PopulateFieldsWithTemplate(loadedTemplate);

                MessageService
                    .Info("Quest template loaded successfully.")
                    .Send();
            }
        }
        catch (Exception ex) {
            MessageService
                .Error($"Error loading template: {ex.Message}")
                .Send();
        }
    }

    private void PopulateFieldsWithTemplate(QuestTemplate template) {
        // Update the current template with the loaded one.
        _template = template;
        _goals.Clear();
        _goalLogics.Clear();

        // Create goal wrappers with IsStartGoal property.
        foreach (var goal in _template.m_goals ?? []) {
            var isStart = _template.m_startGoals?.Any(sg => sg.ToString() == goal.m_goalName?.ToString()) ?? false;
            _goals.Add(new GoalTemplateWrapper(goal, isStart));
        }

        foreach (var logic in _template.m_goalLogic ?? []) {
            _goalLogics.Add(new GoalCompleteLogicWrapper(logic));
        }

        InitializeValues();
    }

}

/// <summary>
/// Wrapper class for GoalTemplate that adds the IsStartGoal property.
/// </summary>
public class GoalTemplateWrapper(GoalTemplate goal, bool isStartGoal) {

    public GoalTemplate Goal { get; } = goal;
    public bool IsStartGoal { get; set; } = isStartGoal;

}

/// <summary>
/// Wrapper class for GoalCompleteLogic to use in UI binding.
/// </summary>
public class GoalCompleteLogicWrapper(GoalCompleteLogic logic) {

    public GoalCompleteLogic Logic { get; } = logic;

    public override string ToString() {
        var parts = new List<string>();

        if (Logic.m_goalsAND?.Count > 0) {
            parts.Add($"AND ({Logic.m_goalsAND.Count} goals)");
        }

        if (Logic.m_goalsOR?.Count > 0) {
            parts.Add($"OR ({Logic.m_requiredORCount} of {Logic.m_goalsOR.Count})");
        }

        if (Logic.m_goalsToAdd?.Count > 0) {
            parts.Add($"Adds {Logic.m_goalsToAdd.Count} goals");
        }

        if (Logic.m_completeQuest) {
            parts.Add("Completes Quest");
        }

        return string.Join(", ", parts);
    }

}