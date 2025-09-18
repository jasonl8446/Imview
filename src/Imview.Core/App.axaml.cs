using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Imview.Core.Views;
using Imview.Core.Common;
using System.Threading.Tasks;

namespace Imview.Core;

public partial class App : Application {

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop) {
        var loginWindow = new LoginWindow();
        
        // Handle the login window closing
        loginWindow.Closed += (sender, e) => {
            if (loginWindow.LoginSuccessful) {
                // Authentication successful, show main window
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.Show();
            } else {
                // Authentication failed or cancelled, exit application
                desktop.Shutdown();
            }
        };
        
        // Show the login window
        loginWindow.Show();
    }
    
}