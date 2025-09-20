// Copyright (c) 2025 Jay Kulsh
// Licensed under the BSD 3-Clause License. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Imview.Core.Database.Models;
using Imview.Core.Services;
using Imview.Core.ViewModels;

namespace Imview.Core.Controls
{
    /// <summary>
    /// Window for editing NPC spell inventories
    /// </summary>
    public partial class NpcSpellInventoryEditor : Window
    {
        private readonly NpcSpellInventoryEditorViewModel _viewModel;
        private bool _isClosing = false;

        public NpcSpellInventoryEditor()
        {
            InitializeComponent();
            _viewModel = new NpcSpellInventoryEditorViewModel(0, "Unknown NPC");
            DataContext = _viewModel;
            SetupEventHandlers();
        }

        public NpcSpellInventoryEditor(ulong npcTemplateId, string npcName, NpcSpellInventory? existingSpellInventory = null)
        {
            InitializeComponent();
            _viewModel = new NpcSpellInventoryEditorViewModel(npcTemplateId, npcName, existingSpellInventory);
            DataContext = _viewModel;
            SetupEventHandlers();
        }

        public bool WasSaved => _viewModel.WasSaved;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupEventHandlers()
        {
            // Handle successful save
            _viewModel.SaveCommand.Subscribe(_ =>
            {
                if (_viewModel.WasSaved)
                {
                    MessageService.Info($"Spell inventory saved successfully for {_viewModel.NpcName}")
                        .WithDuration(TimeSpan.FromSeconds(3))
                        .Send();
                    Close();
                }
            });
            
            // Handle save errors
            _viewModel.SaveCommand.ThrownExceptions.Subscribe(ex =>
            {
                MessageService.Error($"Failed to save spell inventory: {ex.Message}")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .Send();
            });

            // Handle cancel command
            _viewModel.CancelCommand.Subscribe(async _ =>
            {
                if (_isClosing) return; // Prevent re-entry
                
                if (_viewModel.IsDirty)
                {
                    // Show confirmation dialog for unsaved changes
                    var result = await ShowConfirmationDialog(
                        "Unsaved Changes",
                        "You have unsaved changes. Are you sure you want to close without saving?");

                    if (result)
                    {
                        _isClosing = true;
                        Close();
                    }
                }
                else
                {
                    _isClosing = true;
                    Close();
                }
            });

            // Handle window closing event
            Closing += async (sender, e) =>
            {
                // If already closing or was saved, allow close
                if (_isClosing || _viewModel.WasSaved) 
                {
                    return;
                }
                
                // If dirty, show confirmation
                if (_viewModel.IsDirty)
                {
                    e.Cancel = true; // Cancel the close initially
                    
                    var result = await ShowConfirmationDialog(
                        "Unsaved Changes",
                        "You have unsaved changes. Are you sure you want to close without saving?");
                    
                    if (result)
                    {
                        _isClosing = true; // Set flag before calling Close()
                        Close(); // This will trigger Closing event again, but _isClosing=true will allow it
                    }
                    // If result is false, do nothing - window stays open
                }
            };
        }

        private async Task<bool> ShowConfirmationDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 44, 44, 44)) // #2C2C2C
            };

            var stackPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(20)
            };

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 0, 0, 20),
                Foreground = Avalonia.Media.Brushes.White
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };

            var yesButton = new Button
            {
                Content = "Yes",
                Margin = new Avalonia.Thickness(0, 0, 10, 0),
                Padding = new Avalonia.Thickness(20, 8),
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 220, 38, 38)), // Red
                Foreground = Avalonia.Media.Brushes.White
            };

            var noButton = new Button
            {
                Content = "No",
                Padding = new Avalonia.Thickness(20, 8),
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 74, 74, 74)), // Dark gray
                Foreground = Avalonia.Media.Brushes.White
            };

            bool result = false;

            yesButton.Click += (s, e) =>
            {
                result = true;
                dialog.Close();
            };

            noButton.Click += (s, e) =>
            {
                result = false;
                dialog.Close();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(buttonPanel);
            dialog.Content = stackPanel;

            await dialog.ShowDialog(this);
            return result;
        }
    }
}
