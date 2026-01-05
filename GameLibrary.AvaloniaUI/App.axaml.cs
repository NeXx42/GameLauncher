using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.Logic;

namespace GameLibrary.AvaloniaUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        TaskScheduler.UnobservedTaskException += async (s, e) =>
        {
            e.SetObserved();
            await CatchException(e.Exception);
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        //GameLauncher.KillAllExistingProcesses();
    }

    private async Task CatchException(Exception exception)
    {
        // don't want this to loop endlessly loop for some reason
        try
        {
            await DependencyManager.OpenYesNoModal("Unhandled exception", $"{exception.Message}\n\n{exception.StackTrace}");
        }
        catch { }
    }
}