using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using GameLibrary.AvaloniaUI.Controls.Pages.Library;
using GameLibrary.AvaloniaUI.Controls.SubPages.Popup_GameView_Tabs;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.AvaloniaUI.Utils;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.AvaloniaUI.Controls.SubPage;

public partial class Popup_GameView : UserControl
{
    public GameDto? inspectingGame { private set; get; }
    private Popup_GameView_TabBase.TabGroup tabs;

    public Popup_GameView()
    {
        InitializeComponent();

        tabs = new Popup_GameView_TabBase.TabGroup(this,
            new Popup_GameView_Tab_Tags().CreateGroup(groupBtn_Tags, tab_Container),
            new Popup_GameView_Tab_Settings().CreateGroup(groupBtn_Settings, tab_Container),
            new Popup_GameView_Tab_Logs().CreateGroup(groupBtn_Logs, tab_Container)
        );

        btn_Delete.RegisterClick(DeleteGame);
        btn_Overlay.RegisterClick(OpenOverlay);

        btn_Browse.RegisterClick(BrowseToGame);
        btn_Launch.RegisterClick(LaunchGame, "Launching");

        lbl_Title.PointerPressed += async (_, __) => await StartNameChange();

        LibraryManager.onGameDetailsUpdate += RefreshSelectedGame;
        RunnerManager.onGameStatusChange += (a, b) => HelperFunctions.WrapUIThread(() => UpdateRunningGameStatus(a, b));
    }

    public async void Draw(GameDto game)
    {
        inspectingGame = game;
        img_bg.Source = null;

        UpdateRunningGameStatus(game.getAbsoluteBinaryLocation, RunnerManager.IsBinaryRunning(game.getAbsoluteBinaryLocation));

        lbl_Title.Content = game.gameName;
        lbl_LastPlayed.Content = $"Last played {game.GetLastPlayedFormatted()}";

        DrawWarnings();

        await Dispatcher.UIThread.InvokeAsync(() => { });
        await ImageManager.GetGameImage<ImageBrush>(game, UpdateGameIcon);
        await tabs.OpenFresh();
    }

    private void DrawWarnings()
    {
        cont_Warnings.Children.Clear();
        (string, Func<Task>)[] warnings = inspectingGame!.GetWarnings();

        if (warnings.Length > 0)
        {
            foreach (var warning in warnings)
            {
                Common_Button btn = new Common_Button();
                btn.Classes.Add("negative");
                btn.Label = warning.Item1;
                btn.Height = 30;

                btn.RegisterClick(async () => await HandleWarningFix(warning.Item2));
                cont_Warnings.Children.Add(btn);
            }
        }

        async Task HandleWarningFix(Func<Task> body)
        {
            await body();
            RefreshSelectedGame(inspectingGame!.gameId);
        }
    }

    private void UpdateGameIcon(int gameId, ImageBrush? img)
    {
        if (inspectingGame?.gameId != gameId)
            return;

        img_bg.Source = img == null ? null : (IImage)img.Source!;
    }

    private async Task LaunchGame()
    {
        if (RunnerManager.IsBinaryRunning(inspectingGame!.getAbsoluteBinaryLocation))
        {
            RunnerManager.KillProcess(inspectingGame!.getAbsoluteBinaryLocation);
        }
        else
        {
            await inspectingGame!.Launch();
        }
    }

    private void BrowseToGame() => inspectingGame?.BrowseToGame();

    private async Task DeleteGame()
    {
        if (inspectingGame == null)
            return;

        string paragraph = $"Files are located:\n\n{inspectingGame!.getAbsoluteFolderLocation}";
        await DependencyManager.OpenConfirmationAsync("Delete Game?", paragraph,
        [
            ("Remove", async () => await LibraryManager.DeleteGame(inspectingGame, false), "Removing"),
            ("Delete Files", async () => await LibraryManager.DeleteGame(inspectingGame, true), "Deleting"),
        ]);
    }

    private async Task OpenOverlay() => await OverlayManager.LaunchOverlay(inspectingGame!.gameId);

    private async Task StartNameChange()
    {
        string? res = await DependencyManager.OpenStringInputModal("Game Name", inspectingGame!.gameName);

        if (!string.IsNullOrEmpty(res))
            await inspectingGame!.UpdateGameName(res);
    }

    private void RefreshSelectedGame(int gameId)
    {
        if (gameId != inspectingGame?.gameId)
            return;

        Draw(inspectingGame!);
    }

    private void UpdateRunningGameStatus(string binary, bool to)
    {
        if (inspectingGame?.getAbsoluteBinaryLocation != binary)
            return;

        btn_Launch.Label = to ? "Stop" : "Play";
    }
}