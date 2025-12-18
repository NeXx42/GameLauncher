using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Overlay;

public partial class OverlayUI : Window
{
    public OverlayUI()
    {
        InitializeComponent();

        this.WindowState = WindowState.Normal;
        this.ExtendClientAreaToDecorationsHint = true;
        this.SystemDecorations = SystemDecorations.None;
        this.ShowInTaskbar = false;

        if (ConfigHandler.isOnLinux)
        {

        }
        else
        {

        }

        //this.PlatformImpl?.SetWindowType(Avalonia.Controls.Platform.WindowType.Dock);
    }
}