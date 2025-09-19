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
   this software without specific products derived from
   this software without specific prior written permission.
*/

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using Imview.Core.Common.Constants;
using Imview.Core.Services;
using Imview.Core.Views;

namespace Imview.Core.Controls.Templates;

/// <summary>
/// Window for editing individual dialog entries with all their properties
/// </summary>
public class DialogEntryEditorWindow : Window {
    
    private DialogEntryWrapper _result;
    private DialogEntryWrapper _originalEntry;
    private readonly string? _questTitle;
    
    // Basic UI Controls
    private TextBox _personaNameBox = null!;
    private Avalonia.Controls.Button _dialogKeyButton = null!;
    private TextBlock _dialogKeyResolvedText = null!;
    private string _dialogKeyValue = "";
    private TextBox _soundFileBox = null!;
    private NumericUpDown _actorTemplateIdBox = null!;
    private TextBox _cameraNameBox = null!;
    private TextBox _pictureBox = null!;
    private TextBox _actionBox = null!;
    private TextBox _nameOverrideBox = null!;
    private TextBox _guiDisplayBox = null!;
    private TextBox _dialogEventBox = null!;
    
    // Camera Position Controls
    private NumericUpDown _interpolationDurationBox = null!;
    private NumericUpDown _cameraOffsetXBox = null!;
    private NumericUpDown _cameraOffsetYBox = null!;
    private NumericUpDown _cameraOffsetZBox = null!;
    private NumericUpDown _pitchBox = null!;
    private NumericUpDown _yawBox = null!;
    private NumericUpDown _rollBox = null!;
    
    // Camera Shake Controls
    private TextBox _cameraShakeTypeBox = null!;
    private NumericUpDown _cameraShakeDurationBox = null!;
    private NumericUpDown _cameraShakeAmplitudeBox = null!;
    
    // Camera Behavior Controls
    private CheckBox _bypassCameraOnReviewBox = null!;
    private TextBox _cameraZoneNameBox = null!;
    private NumericUpDown _durationBox = null!;
    private NumericUpDown _delayBox = null!;
    private NumericUpDown _cameraHidePlayersBox = null!;
    private NumericUpDown _walkAwayNpcTemplateIDBox = null!;
    
    // Walk Away Controls
    private NumericUpDown _walkAwayExitDirectionBox = null!;
    private NumericUpDown _walkAwayFadeTimeBox = null!;
    private CheckBox _walkAwayUseCurrentFacingBox = null!;
    
    // Secondary Camera Controls
    private TextBox _standInPlayerTagBox = null!;
    private CheckBox _fadeOutCameraBox = null!;
    private CheckBox _snapCameraToPlayerAtExitBox = null!;
    private TextBox _secondaryCameraNameBox = null!;
    private NumericUpDown _secondaryInterpolationDurationBox = null!;
    
    // Animation and NPC Controls
    private ListBox _npcStandInListBox = null!;
    private ListBox _dialogAnimationListBox = null!;
    private ListBox _dialogTurningListBox = null!;
    private NumericUpDown _npcYawOffsetBox = null!;
    private CheckBox _allowPlayerToMoveBox = null!;
    
    // Audio Controls
    private TextBox _soundEffectFileBox = null!;
    private TextBox _musicFileBox = null!;
    private CheckBox _nonStackableMusicBox = null!;
    private CheckBox _nonRepeatableMusicBox = null!;
    private CheckBox _playMusicAtSFXVolumeBox = null!;
    private NumericUpDown _soundEffectDelayBox = null!;
    private NumericUpDown _musicDelayBox = null!;
    private NumericUpDown _musicFadeTimeBox = null!;
    
    // Dialog Behavior Controls
    private CheckBox _dontReleaseCameraAtExitBox = null!;
    private CheckBox _disableBackButtonBox = null!;
    private CheckBox _enableExitButtonBox = null!;
    private TextBox _cameraFadeTypeBox = null!;
    private NumericUpDown _cameraFadeTimeBox = null!;
    private TextBox _idleAnimationBox = null!;
    
    // Spam and Advanced Controls
    private NumericUpDown _spamTimeBox = null!;
    private CheckBox _playSoundIfSpammingBox = null!;
    private CheckBox _playMusicIfSpammingBox = null!;
    private NumericUpDown _stopMusicFadeTimeBox = null!;
    private NumericUpDown _restartMusicFadeTimeBox = null!;
    private CheckBox _meetsRequirementsBox = null!;
    private NumericUpDown _secondaryCameraInitialDelayBox = null!;
    private CheckBox _displayButtonsOnTimedDialogBox = null!;

    public DialogEntryEditorWindow(DialogEntryWrapper entry, string? questTitle = null) {
        _originalEntry = entry;
        _questTitle = questTitle;
        _dialogKeyValue = entry.DialogKey;
        
        // Create a complete copy of all properties
        _result = new DialogEntryWrapper {
            // NPCDialogEntry specific
            PersonaName = entry.PersonaName,
            NameOverride = entry.NameOverride,
            GuiDisplay = entry.GuiDisplay,
            
            // Core Properties
            Requirements = entry.Requirements,
            DialogKey = entry.DialogKey,
            Picture = entry.Picture,
            SoundFile = entry.SoundFile,
            Action = entry.Action,
            DialogEvent = entry.DialogEvent,
            ActorTemplateID = entry.ActorTemplateID,
            CameraName = entry.CameraName,
            
            // Camera Properties
            InterpolationDuration = entry.InterpolationDuration,
            CameraOffsetX = entry.CameraOffsetX,
            CameraOffsetY = entry.CameraOffsetY,
            CameraOffsetZ = entry.CameraOffsetZ,
            Pitch = entry.Pitch,
            Yaw = entry.Yaw,
            Roll = entry.Roll,
            CameraShakeType = entry.CameraShakeType,
            CameraShakeDuration = entry.CameraShakeDuration,
            CameraShakeAmplitude = entry.CameraShakeAmplitude,
            
            // Camera Behavior
            BypassCameraOnReview = entry.BypassCameraOnReview,
            CameraZoneName = entry.CameraZoneName,
            Duration = entry.Duration,
            Delay = entry.Delay,
            CameraHidePlayers = entry.CameraHidePlayers,
            WalkAwayNpcTemplateID = entry.WalkAwayNpcTemplateID,
            
            // Walk Away Properties
            WalkAwayExitDirectionInDegrees = entry.WalkAwayExitDirectionInDegrees,
            WalkAwayFadeTime = entry.WalkAwayFadeTime,
            WalkAwayUseCurrentFacing = entry.WalkAwayUseCurrentFacing,
            
            // Stand-in and Camera
            StandInPlayerTag = entry.StandInPlayerTag,
            FadeOutCamera = entry.FadeOutCamera,
            SnapCameraToPlayerAtExit = entry.SnapCameraToPlayerAtExit,
            SecondaryCameraName = entry.SecondaryCameraName,
            SecondaryInterpolationDuration = entry.SecondaryInterpolationDuration,
            
            // Lists and Arrays
            NpcStandInList = new List<string>(entry.NpcStandInList),
            DialogAnimationList = new List<string>(entry.DialogAnimationList),
            DialogTurningList = new List<string>(entry.DialogTurningList),
            
            // NPC Behavior
            NpcYawOffsetInDegrees = entry.NpcYawOffsetInDegrees,
            AllowPlayerToMove = entry.AllowPlayerToMove,
            
            // Audio Properties
            SoundEffectFile = entry.SoundEffectFile,
            MusicFile = entry.MusicFile,
            NonStackableMusic = entry.NonStackableMusic,
            NonRepeatableMusic = entry.NonRepeatableMusic,
            PlayMusicAtSFXVolume = entry.PlayMusicAtSFXVolume,
            SoundEffectDelay = entry.SoundEffectDelay,
            MusicDelay = entry.MusicDelay,
            MusicFadeTime = entry.MusicFadeTime,
            
            // Camera and Dialog Behavior
            DontReleaseCameraAtExit = entry.DontReleaseCameraAtExit,
            DisableBackButton = entry.DisableBackButton,
            EnableExitButton = entry.EnableExitButton,
            CameraFadeType = entry.CameraFadeType,
            CameraFadeTime = entry.CameraFadeTime,
            IdleAnimation = entry.IdleAnimation,
            
            // Spam and Music Control
            SpamTime = entry.SpamTime,
            PlaySoundIfSpamming = entry.PlaySoundIfSpamming,
            PlayMusicIfSpamming = entry.PlayMusicIfSpamming,
            StopMusicFadeTime = entry.StopMusicFadeTime,
            RestartMusicFadeTime = entry.RestartMusicFadeTime,
            MeetsRequirements = entry.MeetsRequirements,
            
            // Final Properties
            SecondaryCameraInitialDelay = entry.SecondaryCameraInitialDelay,
            DisplayButtonsOnTimedDialog = entry.DisplayButtonsOnTimedDialog
        };

        InitializeWindow();
        InitializeControls();
        PopulateControls();
    }

    private void InitializeWindow() {
        Title = "Edit Dialog Entry - Comprehensive Editor";
        Width = 900;
        Height = 1000;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = true;
        
        // Set window icon and styling
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    }

    private void InitializeControls() {
        var scrollViewer = new ScrollViewer();
        var mainPanel = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Margin = EditorConstants.DEFAULT_MARGIN_THICKNESS
        };

        // Create comprehensive form sections
        mainPanel.Children.Add(CreateBasicInfoSection());
        mainPanel.Children.Add(CreateCameraPositionSection());
        mainPanel.Children.Add(CreateCameraEffectsSection());
        mainPanel.Children.Add(CreateTimingAndBehaviorSection());
        mainPanel.Children.Add(CreateWalkAwaySection());
        mainPanel.Children.Add(CreateAnimationSection());
        mainPanel.Children.Add(CreateAudioSection());
        mainPanel.Children.Add(CreateDialogBehaviorSection());
        mainPanel.Children.Add(CreateAdvancedSection());
        mainPanel.Children.Add(CreateActionButtons());

        scrollViewer.Content = mainPanel;
        Content = scrollViewer;
    }

    private Control CreateBasicInfoSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Persona Name:", 
                    "The name of the NPC persona that will speak this dialogue", 
                    _personaNameBox = new TextBox()),
                    
                CreateDialogKeySection(),
                    
                CreateLabeledControl("Sound File Path:", 
                    "Path to the audio file for voice acting (e.g., '|Sound_Dialogue|WorldData|Sound/Dialogue/MerleAmbrose_QST_13.mp3')", 
                    _soundFileBox = new TextBox()),
                    
                CreateLabeledControl("Actor Template ID:", 
                    "Unique identifier for the NPC actor template (0 = no specific actor)", 
                    _actorTemplateIdBox = new NumericUpDown { 
                        Minimum = 0, 
                        Maximum = uint.MaxValue,
                        Value = 0 
                    }),

                CreateLabeledControl("Picture/Icon:", 
                    "Image or icon to display with this dialogue", 
                    _pictureBox = new TextBox()),
                    
                CreateLabeledControl("Action:", 
                    "Special action or animation to perform during dialogue", 
                    _actionBox = new TextBox()),
                    
                CreateLabeledControl("Name Override:", 
                    "Override the displayed name of the speaker", 
                    _nameOverrideBox = new TextBox()),
                    
                CreateLabeledControl("GUI Display:", 
                    "Special GUI display settings for this dialogue", 
                    _guiDisplayBox = new TextBox()),
                    
                CreateLabeledControl("Dialog Event:", 
                    "Event triggered by this dialog entry", 
                    _dialogEventBox = new TextBox())
            }
        };

        return CreateGroupBox("Basic Dialog Properties", content);
    }

    private Control CreateDialogKeySection() {
        var panel = new StackPanel { Spacing = 3 };
        
        var headerText = new TextBlock { 
            Text = "Dialog Locale Key:",
            FontWeight = FontWeight.SemiBold
        };
        
        var explanationTextBlock = new TextBlock {
            Text = "The localization key that references the actual dialogue text (e.g., 'WizQst9559_00000129')",
            Foreground = Brushes.LightGray,
            FontStyle = FontStyle.Italic,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 5)
        };
        
        // Button to open locale selection popup
        _dialogKeyButton = new Avalonia.Controls.Button {
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 8),
            MinHeight = 32
        };
        
        _dialogKeyButton.Click += async (sender, e) => {
            var preferredCategory = GetDialogCategory();
            var popup = new LocaleSelectionPopup(preferredCategory, _dialogKeyValue);
            var result = await popup.ShowDialog<string?>(this);
            
            if (!string.IsNullOrEmpty(result)) {
                _dialogKeyValue = result;
                UpdateDialogKeyDisplay();
            }
        };
        
        // Text block to display the resolved English text
        _dialogKeyResolvedText = new TextBlock {
            Foreground = Brushes.LightGray,
            FontStyle = FontStyle.Italic,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        panel.Children.Add(headerText);
        panel.Children.Add(explanationTextBlock);
        panel.Children.Add(_dialogKeyButton);
        panel.Children.Add(_dialogKeyResolvedText);
        
        UpdateDialogKeyDisplay();
        
        return panel;
    }

    private string? GetDialogCategory() {
        if (string.IsNullOrEmpty(_questTitle)) {
            return null;
        }

        // Extract ID from quest title like "QuestTitle_1625BF" -> "1625BF"
        var underscoreIndex = _questTitle.LastIndexOf('_');
        if (underscoreIndex == -1) {
            return null;
        }

        var questId = _questTitle.Substring(underscoreIndex + 1);
        return $"WizQst{questId}";
    }

    private void UpdateDialogKeyDisplay() {
        // Update button content
        _dialogKeyButton.Content = string.IsNullOrEmpty(_dialogKeyValue) ? 
            "(Click to select dialog key)" : _dialogKeyValue;

        // Update resolved text
        if (string.IsNullOrEmpty(_dialogKeyValue)) {
            _dialogKeyResolvedText.Text = "";
        } else {
            var resolvedText = ResolveLocaleString(_dialogKeyValue);
            if (!string.IsNullOrEmpty(resolvedText) && resolvedText != _dialogKeyValue) {
                _dialogKeyResolvedText.Text = $"➤ {resolvedText}";
            } else {
                _dialogKeyResolvedText.Text = "➤ (No locale text found)";
            }
        }
    }

    private string? ResolveLocaleString(string localeReference) {
        if (string.IsNullOrEmpty(localeReference) || !LocaleService.Instance.IsLoaded) {
            return null;
        }

        // Split the locale reference into category and key (e.g., "WizQst9559_00000129")
        var underscoreIndex = localeReference.LastIndexOf('_');
        if (underscoreIndex == -1) {
            return null; // Invalid format
        }

        var category = localeReference.Substring(0, underscoreIndex);
        var key = localeReference.Substring(underscoreIndex + 1);

        // Pad the key to 8 digits if it's not already and is all numeric
        if (key.Length < 8 && key.All(char.IsDigit)) {
            key = key.PadLeft(8, '0');
        }

        return LocaleService.Instance.GetString(category, key);
    }

    private Control CreateAdvancedSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Camera Name:", 
                    "Camera angle or position for this dialogue (optional)", 
                    _cameraNameBox = new TextBox()),
                    
                CreateLabeledControl("Picture/Icon:", 
                    "Image or icon to display with this dialogue (e.g., GUI/NpcPortraits/Art_Portrait_Ambrose.dds)", 
                    _pictureBox = new TextBox()),
                    
                CreateLabeledControl("Action:", 
                    "Special action or animation to perform during dialogue (optional)", 
                    _actionBox = new TextBox()),
                    
                CreateLabeledControl("Name Override:", 
                    "Override the displayed name of the speaker (optional)", 
                    _nameOverrideBox = new TextBox()),
                    
                CreateLabeledControl("GUI Display:", 
                    "Special GUI display settings for this dialogue (optional)", 
                    _guiDisplayBox = new TextBox())
            }
        };

        return CreateGroupBox("Advanced Properties", content);
    }

    private Control CreateExtendedSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Dialog Event:", 
                    "Event triggered by this dialog entry (optional)", 
                    _dialogEventBox = new TextBox()),
                    
                CreateLabeledControl("Duration (seconds):", 
                    "How long this dialog entry should be displayed (0 = no limit)", 
                    _durationBox = new NumericUpDown { 
                        Minimum = 0, 
                        Maximum = 999,
                        Value = 0,
                        Increment = 0.1m,
                        FormatString = "F1"
                    }),
                    
                CreateLabeledControl("Delay (seconds):", 
                    "Delay before showing this dialog entry", 
                    _delayBox = new NumericUpDown { 
                        Minimum = 0, 
                        Maximum = 999,
                        Value = 0,
                        Increment = 0.1m,
                        FormatString = "F1"
                    }),
                    
                CreateLabeledControl("Sound Effect File:", 
                    "Path to sound effect file to play during this dialog", 
                    _soundEffectFileBox = new TextBox()),
                    
                CreateLabeledControl("Music File:", 
                    "Path to background music file to play during this dialog", 
                    _musicFileBox = new TextBox()),
                    
                CreateBehaviorSection()
            }
        };

        return CreateGroupBox("Extended Properties", content);
    }

    private Control CreateBehaviorSection() {
        _allowPlayerToMoveBox = new CheckBox { 
            Content = "Allow Player to Move",
            Margin = new Thickness(10, 0, 0, 0)
        };
        
        _disableBackButtonBox = new CheckBox { 
            Content = "Disable Back Button",
            Margin = new Thickness(10, 0, 0, 0)
        };
        
        _enableExitButtonBox = new CheckBox { 
            Content = "Enable Exit Button",
            Margin = new Thickness(10, 0, 0, 0)
        };

        return new StackPanel {
            Spacing = 5,
            Children = {
                new TextBlock { 
                    Text = "Dialog Behavior Options:",
                    FontWeight = FontWeight.SemiBold
                },
                _allowPlayerToMoveBox,
                _disableBackButtonBox,
                _enableExitButtonBox
            }
        };
    }

    private Control CreateCameraPositionSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Camera Name:", 
                    "Camera angle or position for this dialogue", 
                    _cameraNameBox = new TextBox()),
                    
                CreateLabeledControl("Interpolation Duration:", 
                    "Camera movement interpolation duration", 
                    _interpolationDurationBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Camera Offset X:", 
                    "Camera X-axis offset position", 
                    _cameraOffsetXBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Camera Offset Y:", 
                    "Camera Y-axis offset position", 
                    _cameraOffsetYBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Camera Offset Z:", 
                    "Camera Z-axis offset position", 
                    _cameraOffsetZBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Pitch (degrees):", 
                    "Camera pitch rotation", 
                    _pitchBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Yaw (degrees):", 
                    "Camera yaw rotation", 
                    _yawBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Roll (degrees):", 
                    "Camera roll rotation", 
                    _rollBox = CreateFloatNumericUpDown())
            }
        };
        return CreateGroupBox("Camera Position", content);
    }

    private Control CreateCameraEffectsSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Camera Shake Type:", 
                    "Type of camera shake effect", 
                    _cameraShakeTypeBox = new TextBox()),
                    
                CreateLabeledControl("Camera Shake Duration:", 
                    "Duration of camera shake effect", 
                    _cameraShakeDurationBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Camera Shake Amplitude:", 
                    "Intensity/amplitude of camera shake", 
                    _cameraShakeAmplitudeBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Camera Zone Name:", 
                    "Name of camera zone for this dialog", 
                    _cameraZoneNameBox = new TextBox()),
                    
                CreateLabeledControl("Camera Fade Type:", 
                    "Type of camera fade effect", 
                    _cameraFadeTypeBox = new TextBox()),
                    
                CreateLabeledControl("Camera Fade Time:", 
                    "Duration of camera fade effect", 
                    _cameraFadeTimeBox = CreateFloatNumericUpDown()),

                (_bypassCameraOnReviewBox = new CheckBox { Content = "Bypass Camera on Review" }),
                (_fadeOutCameraBox = new CheckBox { Content = "Fade Out Camera" }),
                (_snapCameraToPlayerAtExitBox = new CheckBox { Content = "Snap Camera to Player at Exit" }),
                (_dontReleaseCameraAtExitBox = new CheckBox { Content = "Don't Release Camera at Exit" })
            }
        };
        return CreateGroupBox("Camera Effects", content);
    }

    private Control CreateTimingAndBehaviorSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Duration (seconds):", 
                    "How long this dialog entry should be displayed", 
                    _durationBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Delay (seconds):", 
                    "Delay before showing this dialog entry", 
                    _delayBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Camera Hide Players:", 
                    "Camera setting for hiding players", 
                    _cameraHidePlayersBox = new NumericUpDown { Minimum = 0, Maximum = int.MaxValue }),
                    
                CreateLabeledControl("Spam Time:", 
                    "Anti-spam time setting", 
                    _spamTimeBox = CreateFloatNumericUpDown()),

                (_allowPlayerToMoveBox = new CheckBox { Content = "Allow Player to Move" }),
                (_disableBackButtonBox = new CheckBox { Content = "Disable Back Button" }),
                (_enableExitButtonBox = new CheckBox { Content = "Enable Exit Button" }),
                (_displayButtonsOnTimedDialogBox = new CheckBox { Content = "Display Buttons on Timed Dialog" }),
                (_meetsRequirementsBox = new CheckBox { Content = "Meets Requirements" })
            }
        };
        return CreateGroupBox("Timing & Behavior", content);
    }

    private Control CreateWalkAwaySection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Walk Away NPC Template ID:", 
                    "NPC template ID for walk-away behavior", 
                    _walkAwayNpcTemplateIDBox = new NumericUpDown { Minimum = 0, Maximum = uint.MaxValue }),
                    
                CreateLabeledControl("Walk Away Exit Direction (degrees):", 
                    "Direction in degrees for NPC walk-away exit", 
                    _walkAwayExitDirectionBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Walk Away Fade Time:", 
                    "Fade time for walk-away effect", 
                    _walkAwayFadeTimeBox = CreateFloatNumericUpDown()),

                (_walkAwayUseCurrentFacingBox = new CheckBox { Content = "Walk Away Use Current Facing" })
            }
        };
        return CreateGroupBox("Walk Away Behavior", content);
    }

    private Control CreateAnimationSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Stand-in Player Tag:", 
                    "Player tag for stand-in functionality", 
                    _standInPlayerTagBox = new TextBox()),
                    
                CreateLabeledControl("Secondary Camera Name:", 
                    "Secondary camera name", 
                    _secondaryCameraNameBox = new TextBox()),
                    
                CreateLabeledControl("Secondary Interpolation Duration:", 
                    "Secondary camera interpolation duration", 
                    _secondaryInterpolationDurationBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Secondary Camera Initial Delay:", 
                    "Initial delay for secondary camera", 
                    _secondaryCameraInitialDelayBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Idle Animation:", 
                    "Idle animation name", 
                    _idleAnimationBox = new TextBox()),
                    
                CreateLabeledControl("NPC Yaw Offset (degrees):", 
                    "NPC yaw offset in degrees", 
                    _npcYawOffsetBox = CreateFloatNumericUpDown()),

                CreateListSection("NPC Stand-in List:", "List of NPC stand-ins", ref _npcStandInListBox),
                CreateListSection("Dialog Animation List:", "List of dialog animations", ref _dialogAnimationListBox),
                CreateListSection("Dialog Turning List:", "List of dialog turning animations", ref _dialogTurningListBox)
            }
        };
        return CreateGroupBox("Animation & NPCs", content);
    }

    private Control CreateAudioSection() {
        var content = new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                CreateLabeledControl("Sound Effect File:", 
                    "Path to sound effect file to play during this dialog", 
                    _soundEffectFileBox = new TextBox()),
                    
                CreateLabeledControl("Music File:", 
                    "Path to background music file to play during this dialog", 
                    _musicFileBox = new TextBox()),
                    
                CreateLabeledControl("Sound Effect Delay:", 
                    "Delay before playing sound effect", 
                    _soundEffectDelayBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Music Delay:", 
                    "Delay before playing music", 
                    _musicDelayBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Music Fade Time:", 
                    "Music fade time", 
                    _musicFadeTimeBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Stop Music Fade Time:", 
                    "Fade time when stopping music", 
                    _stopMusicFadeTimeBox = CreateFloatNumericUpDown()),
                    
                CreateLabeledControl("Restart Music Fade Time:", 
                    "Fade time when restarting music", 
                    _restartMusicFadeTimeBox = CreateFloatNumericUpDown()),

                (_nonStackableMusicBox = new CheckBox { Content = "Non-stackable Music" }),
                (_nonRepeatableMusicBox = new CheckBox { Content = "Non-repeatable Music" }),
                (_playMusicAtSFXVolumeBox = new CheckBox { Content = "Play Music at SFX Volume" }),
                (_playSoundIfSpammingBox = new CheckBox { Content = "Play Sound if Spamming" }),
                (_playMusicIfSpammingBox = new CheckBox { Content = "Play Music if Spamming" })
            }
        };
        return CreateGroupBox("Audio Settings", content);
    }

    private Control CreateDialogBehaviorSection() {
        // This replaces the old CreateBehaviorSection and CreateExtendedSection
        return CreateGroupBox("Dialog Behavior", new StackPanel {
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            Children = {
                new TextBlock { 
                    Text = "Dialog behavior and interaction options",
                    FontStyle = FontStyle.Italic,
                    Foreground = Brushes.LightGray
                }
            }
        });
    }

    private static NumericUpDown CreateFloatNumericUpDown() {
        return new NumericUpDown {
            Minimum = -9999,
            Maximum = 9999,
            Value = 0,
            Increment = 0.1m,
            FormatString = "F2"
        };
    }

    private Control CreateListSection(string title, string description, ref ListBox listBox) {
        listBox = new ListBox { Height = 80 };
        
        var addButton = new Button { Content = "Add", MinWidth = 60 };
        var removeButton = new Button { Content = "Remove", MinWidth = 60 };
        
        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Children = { addButton, removeButton }
        };

        return new StackPanel {
            Spacing = 3,
            Children = {
                new TextBlock { Text = title, FontWeight = FontWeight.SemiBold },
                new TextBlock { 
                    Text = description,
                    Foreground = Brushes.LightGray,
                    FontStyle = FontStyle.Italic,
                    FontSize = 11
                },
                listBox,
                buttonPanel
            }
        };
    }

    private Control CreateActionButtons() {
        return new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0),
            Children = {
                new Button {
                    Content = "Save",
                    Padding = new Thickness(20, 10),
                    Command = ReactiveCommand.Create(SaveAndClose)
                },
                new Button {
                    Content = "Cancel",
                    Padding = new Thickness(20, 10),
                    Command = ReactiveCommand.Create(() => Close(null))
                }
            }
        };
    }

    private static Control CreateLabeledControl(string labelText, string explanationText, Control control) {
        var panel = new StackPanel { 
            Spacing = 3
        };
        
        var headerText = new TextBlock { 
            Text = labelText,
            FontWeight = FontWeight.SemiBold
        };
        
        var explanationTextBlock = new TextBlock {
            Text = explanationText,
            Foreground = Brushes.LightGray,
            FontStyle = FontStyle.Italic,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 5)
        };
        
        panel.Children.Add(headerText);
        panel.Children.Add(explanationTextBlock);
        panel.Children.Add(control);
        
        return panel;
    }

    private static Border CreateGroupBox(string header, Control content) =>
        new() {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = EditorConstants.DEFAULT_CORNER_RADIUS,
            Padding = EditorConstants.DEFAULT_GROUP_PADDING,
            Margin = new Thickness(0, 0, 0, 15),
            Child = new StackPanel {
                Spacing = EditorConstants.DEFAULT_CONTROL_SPACING,
                Children = {
                    new TextBlock {
                        Text = header,
                        FontWeight = FontWeight.Bold,
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 8)
                    },
                    content
                }
            }
        };

    private void PopulateControls() {
        _personaNameBox.Text = _result.PersonaName;
        _dialogKeyValue = _result.DialogKey;
        UpdateDialogKeyDisplay();
        _soundFileBox.Text = _result.SoundFile;
        _actorTemplateIdBox.Value = (decimal)_result.ActorTemplateID;
        _cameraNameBox.Text = _result.CameraName;
        _pictureBox.Text = _result.Picture;
        _actionBox.Text = _result.Action;
        _nameOverrideBox.Text = _result.NameOverride;
        _guiDisplayBox.Text = _result.GuiDisplay;
        
        // Extended properties
        _dialogEventBox.Text = _result.DialogEvent;
        _durationBox.Value = (decimal)_result.Duration;
        _delayBox.Value = (decimal)_result.Delay;
        _soundEffectFileBox.Text = _result.SoundEffectFile;
        _musicFileBox.Text = _result.MusicFile;
        _allowPlayerToMoveBox.IsChecked = _result.AllowPlayerToMove;
        _disableBackButtonBox.IsChecked = _result.DisableBackButton;
        _enableExitButtonBox.IsChecked = _result.EnableExitButton;
    }

    private void SaveAndClose() {
        // Update the result with current values
        _result.PersonaName = _personaNameBox.Text ?? "";
        _result.DialogKey = _dialogKeyValue ?? "";
        _result.SoundFile = _soundFileBox.Text ?? "";
        _result.ActorTemplateID = (uint)(_actorTemplateIdBox.Value ?? 0);
        _result.CameraName = _cameraNameBox.Text ?? "";
        _result.Picture = _pictureBox.Text ?? "";
        _result.Action = _actionBox.Text ?? "";
        _result.NameOverride = _nameOverrideBox.Text ?? "";
        _result.GuiDisplay = _guiDisplayBox.Text ?? "";
        
        // Extended properties
        _result.DialogEvent = _dialogEventBox.Text ?? "";
        _result.Duration = (float)(_durationBox.Value ?? 0);
        _result.Delay = (float)(_delayBox.Value ?? 0);
        _result.SoundEffectFile = _soundEffectFileBox.Text ?? "";
        _result.MusicFile = _musicFileBox.Text ?? "";
        _result.AllowPlayerToMove = _allowPlayerToMoveBox.IsChecked ?? false;
        _result.DisableBackButton = _disableBackButtonBox.IsChecked ?? false;
        _result.EnableExitButton = _enableExitButtonBox.IsChecked ?? false;

        Close(_result);
    }
}