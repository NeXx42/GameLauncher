using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class Library_ActiveProcess : UserControl
{
    private string? representing;

    public Library_ActiveProcess()
    {
        InitializeComponent();

        this.PointerPressed += (_, __) => TryToClose();
    }

    public void Draw(string lbl)
    {
        this.representing = lbl;
        this.lbl.Content = lbl;
    }

    private async void TryToClose()
    {
        if (string.IsNullOrEmpty(representing))
            return; // maybe have a ui clean up here

        if (!await DependencyManager.OpenYesNoModal("Close Game?", $"Are you sure you want to force quit '{representing}'?"))
            return;

        RunnerManager.KillProcess(representing);
    }
}