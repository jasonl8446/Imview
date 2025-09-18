using Avalonia.Controls;
using Avalonia.Interactivity;
using Imview.Core.ViewModels;
using Imview.Core.Authentication.Models;
using System;

namespace Imview.Core.Views;

public partial class LoginWindow : Window {
    public LoginViewModel ViewModel { get; }
    public AuthenticationResult? AuthenticationResult { get; private set; }
    public bool LoginSuccessful { get; private set; }

    public LoginWindow() {
        InitializeComponent();
        
        ViewModel = new LoginViewModel();
        DataContext = ViewModel;
        
        // Subscribe to authentication completed event
        ViewModel.AuthenticationCompleted += OnAuthenticationCompleted;
        
        // Handle window events
        Closing += OnClosing;
    }

    private void OnAuthenticationCompleted(object? sender, AuthenticationResult result) {
        AuthenticationResult = result;
        LoginSuccessful = result.Success;
        
        if (result.Success) {
            // Close the window on successful login
            Close(true);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e) {
        // Clean up the view model
        ViewModel.AuthenticationCompleted -= OnAuthenticationCompleted;
        ViewModel.Dispose();
    }

    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        
        // Focus the username field if empty, otherwise focus password
        var usernameBox = this.FindControl<TextBox>("UsernameTextBox");
        var passwordBox = this.FindControl<TextBox>("PasswordTextBox");
        
        if (string.IsNullOrWhiteSpace(ViewModel.Username)) {
            usernameBox?.Focus();
        } else {
            passwordBox?.Focus();
        }
    }

    private async void TestButton_Click(object? sender, RoutedEventArgs e) {
        // Debug: Direct method call to bypass command issues
        System.Diagnostics.Debug.WriteLine("Test button clicked!");
        System.Diagnostics.Debug.WriteLine($"Server Address: '{ViewModel.ServerAddress}'");
        
        // Call the test method directly
        try {
            await ViewModel.TestConnectionAsync();
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Error executing test: {ex}");
            ViewModel.StatusMessage = $"Test failed: {ex.Message}";
        }
    }
}