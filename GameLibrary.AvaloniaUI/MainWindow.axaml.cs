using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GameLibrary.AvaloniaUI.Controls.Pages;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.AvaloniaUI.Utils;
using GameLibrary.Controller;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;

namespace GameLibrary.AvaloniaUI;

public partial class MainWindow : Window, IControllerInputCallback
{
    public static MainWindow? instance { private set; get; }

    private UserControl? activePage;

    public MainWindow()
    {
        instance = this;

        InitializeComponent();

        DragDrop.SetAllowDrop(this, true);
        cont_Modals.IsVisible = false;

        ControllerInputHandler.Init(this);

        _ = OnStartAsync();
    }

    private async Task OnStartAsync()
    {
        await DependencyManager.PreSetup(new UILinker(), new AvaloniaImageBrushFetcher());
        string? passwordHash = ConfigHandler.configProvider!.GetValue(ConfigKeys.PasswordHash);

        if (!string.IsNullOrEmpty(passwordHash))
        {
            EnterPage<Page_Login>().Enter(passwordHash, async () => await CompleteLoadAsync());
        }
        else
        {
            await CompleteLoadAsync();
        }
    }

    private async Task CompleteLoadAsync()
    {
        await DependencyManager.PostSetup();
        EnterPage<Page_Library>();
    }


    private T EnterPage<T>() where T : UserControl
    {
        if (activePage != null)
        {
            cont_Pages.Children.Remove(activePage);
        }

        T page = Activator.CreateInstance<T>();

        activePage = page;
        activePage.ZIndex = 1;

        cont_Pages.Children.Add(activePage);

        return page;
    }

    public async Task DisplayModalAsync<T>(Func<T, Task> modalReq) where T : UserControl
    {
        cont_Modals.IsVisible = true;
        cont_Pages.Effect = new ImmutableBlurEffect(5);

        T modal = Activator.CreateInstance<T>();
        cont_Modals.Child = modal;

        await modalReq(modal);

        cont_Modals.Child = null;
        cont_Modals.IsVisible = false;
        cont_Pages.Effect = null;
    }

    public void Move(int x, int y)
    {
        if (activePage is Page_Library lib)
        {
            Dispatcher.UIThread.Post(() => _ = lib.Move(x, y));
        }
    }

    public void PressButton(ControllerButton btn)
    {
        if (activePage is Page_Library lib)
        {
            Dispatcher.UIThread.Post(() => _ = lib.PressButton(btn));
        }
    }
}