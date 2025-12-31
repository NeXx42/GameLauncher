using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += CatchException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        GameLauncher.KillAllExistingProcesses();
    }

    private async void CatchException(object sender, UnhandledExceptionEventArgs arg)
    {
        // don't want this to loop endlessly loop for some reason
        try
        {
            if (arg.ExceptionObject is Exception e)
            {
                await DialogHelper.OpenExceptionDialog(e);
            }
        }
        catch { }
    }
}