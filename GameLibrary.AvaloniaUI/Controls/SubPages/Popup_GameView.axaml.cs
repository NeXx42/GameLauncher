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
using GameLibrary.AvaloniaUI.Controls.Pages.Library;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.AvaloniaUI.Utils;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.SubPage;

public partial class Popup_GameView : UserControl
{
    private GameDto? inspectingGame;
    private GameView_Tabs tabs;

    public Popup_GameView()
    {
        InitializeComponent();

        tabs = new GameView_Tabs(this, new GameView_TabGroup.Tab_Tags(tab_Tags, groupBtn_Tags), new GameView_TabGroup.Tab_LaunchSettings(tab_Settings, groupBtn_Settings), new GameView_TabGroup.Tab_Logs(tab_Logs, groupBtn_Logs));

        btn_Delete.RegisterClick(DeleteGame);
        btn_Overlay.RegisterClick(OpenOverlay);

        btn_Browse.RegisterClick(BrowseToGame);
        btn_Launch.RegisterClick(LaunchGame, "Launching");

        lbl_Title.PointerPressed += async (_, __) => await StartNameChange();

        ImageManager.RegisterOnGlobalImageChange<ImageBrush>(UpdateGameIcon);
        LibraryHandler.onGameDetailsUpdate += async (int id) => await RefreshSelectedGame(id);

        RunnerManager.onGameStatusChange += (a, b) => HelperFunctions.WrapUIThread(() => UpdateRunningGameStatus(a, b));
    }

    public async Task Draw(GameDto game)
    {
        inspectingGame = game;
        img_bg.Background = null;

        UpdateRunningGameStatus(game.getAbsoluteBinaryLocation, RunnerManager.IsBinaryRunning(game.getAbsoluteBinaryLocation));

        lbl_Title.Content = game.gameName;
        lbl_LastPlayed.Content = $"Last played {game.GetLastPlayedFormatted()}";

        await ImageManager.GetGameImage<ImageBrush>(game, UpdateGameIcon);
        await tabs.OpenFresh();


        (int? currentExecutable, string[] possibleBinaries)? options = game.GetPossibleBinaries();

        if (options != null)
        {
            inp_binary.IsVisible = true;
            inp_binary.SetupAsync(options.Value.possibleBinaries.Select(x => Path.GetFileName(x)), options.Value.currentExecutable, HandleBinaryChange);
        }
        else
        {
            inp_binary.IsVisible = false;
        }
    }

    private void UpdateGameIcon(int gameId, ImageBrush? img)
    {
        if (inspectingGame?.gameId != gameId)
            return;

        img_bg.Background = img;
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
        await DependencyManager.uiLinker!.OpenConfirmationAsync("Delete Game?", paragraph,
        [
            ("Remove", async () => await LibraryHandler.DeleteGame(inspectingGame, false), "Removing"),
            ("Delete Files", async () => await LibraryHandler.DeleteGame(inspectingGame, true), "Deleting"),
        ]);
    }

    private async Task OpenOverlay() => await OverlayManager.LaunchOverlay(inspectingGame!.gameId);
    private async Task HandleBinaryChange() => await inspectingGame!.ChangeBinaryLocation(inp_binary.selectedValue?.ToString());

    private async Task StartNameChange()
    {
        string? res = await DependencyManager.uiLinker!.OpenStringInputModal("Game Name", inspectingGame!.gameName);

        if (!string.IsNullOrEmpty(res))
            await inspectingGame!.UpdateGameName(res);
    }

    private async Task RefreshSelectedGame(int gameId)
    {
        if (gameId != inspectingGame?.gameId)
            return;

        await Draw(inspectingGame!);
    }

    private void UpdateRunningGameStatus(string binary, bool to)
    {
        if (inspectingGame?.getAbsoluteBinaryLocation != binary)
            return;

        btn_Launch.Label = to ? "Stop" : "Play";
    }



    // Tab groups

    private class GameView_Tabs : UITabGroup
    {
        public Popup_GameView master;

        public GameView_Tabs(Popup_GameView master, params GameView_TabGroup[] tabs) : base()
        {
            this.groups = tabs;
            this.master = master;

            for (int i = 0; i < this.groups.Length; i++)
                this.groups[i].Setup(this, i);
        }

        public async Task OpenFresh()
        {
            int temp = selectedGroup ?? 0;
            selectedGroup = null;

            await ChangeSelection(temp);
        }

        public override async Task ChangeSelection(int to)
        {
            await base.ChangeSelection(to);
        }
    }

    // groups

    private abstract class GameView_TabGroup : UITabGroup_Group
    {
        protected GameView_Tabs? master;
        protected Common_ButtonToggle toggleBtn;

        protected int? lastGameId;
        protected GameDto inspectingGame => master!.master.inspectingGame!;

        public GameView_TabGroup(Control element, Common_ButtonToggle btn) : base(element, btn)
        {
            toggleBtn = btn;
            btn.autoToggle = false;
        }

        public sealed override void Setup(UITabGroup master, int index)
        {
            this.master = (GameView_Tabs)master;

            toggleBtn.Register(async (_) => await master.ChangeSelection(index));
            element.IsVisible = false;

            InternalSetup(this.master);
        }

        public sealed override Task Close()
        {
            toggleBtn.isSelected = false;
            return base.Close();
        }

        public sealed override async Task Open()
        {
            toggleBtn.isSelected = true;

            await base.Open();
            await OpenWithGame(inspectingGame, inspectingGame.gameId != lastGameId);
            lastGameId = inspectingGame?.gameId;
        }

        protected abstract void InternalSetup(GameView_Tabs master);
        protected abstract Task OpenWithGame(GameDto? game, bool isNewGame);


        // Tags

        internal class Tab_Tags : GameView_TabGroup
        {
            private Dictionary<int, Library_Tag> allTags = new Dictionary<int, Library_Tag>();

            public Tab_Tags(Control element, Common_ButtonToggle btn) : base(element, btn)
            {
            }

            protected override void InternalSetup(GameView_Tabs master) { }

            protected override async Task OpenWithGame(GameDto? game, bool isNewGame)
            {
                if (isNewGame)
                {
                    await CheckForNewTags();
                    await RedrawSelectedTags(game!);
                }
            }

            public async Task CheckForNewTags()
            {
                int[] newTags = await LibraryHandler.GetAllTags();

                if (allTags.Count == newTags.Length)
                    return;

                allTags.Clear();
                master!.master.cont_AllTags.Children.Clear();

                foreach (int tagId in newTags)
                {
                    GenerateTag(tagId);
                }

                void GenerateTag(int tagId)
                {
                    dbo_Tag? tag = LibraryHandler.GetTagById(tagId);

                    if (tag != null)
                    {
                        Library_Tag tagUI = new Library_Tag();
                        tagUI.Draw(tag, HandleTagToggle);

                        master.master.cont_AllTags.Children.Add(tagUI);
                        allTags.Add(tagId, tagUI);
                    }
                }
            }

            private async void HandleTagToggle(int tagId)
            {
                await inspectingGame!.ToggleTag(tagId);
                await RedrawSelectedTags(inspectingGame);
            }

            private async Task RedrawSelectedTags(GameDto game)
            {
                foreach (KeyValuePair<int, Library_Tag> tag in allTags)
                {
                    tag.Value.Margin = new Thickness(0, 0, 0, 5);
                    tag.Value.Toggle(game?.tags.Contains(tag.Key) ?? false);
                }
            }
        }

        // Settings

        internal class Tab_LaunchSettings : GameView_TabGroup
        {
            private List<(int id, string name)>? possibleRunners;

            public Tab_LaunchSettings(Control element, Common_ButtonToggle btn) : base(element, btn)
            {
            }

            protected override void InternalSetup(GameView_Tabs master)
            {
                master.master.inp_Emulate.RegisterOnChange(HandleEmulateToggle);
                master.master.inp_CaptureLogs.RegisterOnChange(HandleCaptureLogs);
            }

            protected override async Task OpenWithGame(GameDto? game, bool isNewGame)
            {
                if (isNewGame)
                {
                    possibleRunners = await RunnerManager.GetRunnerProfiles();// await DatabaseHandler.GetItems<dbo_WineProfile>(QueryBuilder.OrderBy(nameof(dbo_WineProfile.isDefault), true));
                    string firstProfile = possibleRunners.Count > 0 ? possibleRunners[0].name : "INVALID";

                    string[] profileOptions = [$"Default ({firstProfile})", .. possibleRunners!.Select(x => x.name)!.ToArray()];
                    int selectedProfile = possibleRunners.Select(x => x.id).ToList().IndexOf(game?.runnerId ?? -1);

                    master!.master.inp_WineProfile.IsVisible = true;
                    master.master.inp_WineProfile.SetupAsync(profileOptions, selectedProfile >= 0 ? (selectedProfile + 1) : 0, HandleWineProfileChange);

                }

                master!.master.inp_Emulate.SilentSetValue(game!.useRegionEmulation ?? false);
                master.master.inp_CaptureLogs.SilentSetValue(game!.captureLogs ?? false);
            }

            private async Task HandleEmulateToggle(bool to) => await inspectingGame!.UpdateGameEmulationStatus(to);
            private async Task HandleCaptureLogs(bool to) => await inspectingGame!.UpdateCaptureLogsStatus(to);

            private async Task HandleWineProfileChange()
            {
                int? newProfileId = null;
                int selectedIndex = master!.master.inp_WineProfile.selectedIndex;

                if (selectedIndex != 0) // default profile
                {
                    newProfileId = possibleRunners![selectedIndex - 1].id;
                }

                await inspectingGame!.ChangeRunnerId(newProfileId);
            }
        }

        // Logs

        internal class Tab_Logs : GameView_TabGroup
        {
            public Tab_Logs(Control element, Common_ButtonToggle btn) : base(element, btn)
            {
            }

            protected override void InternalSetup(GameView_Tabs master) { }

            protected override async Task OpenWithGame(GameDto? game, bool isNewGame)
            {
                await RefreshLogs(game);
            }

            private async Task RefreshLogs(GameDto? game)
            {
                if (game == null)
                {
                    master!.master.lbl_Logs.Text = "";
                    return;
                }

                master!.master.lbl_Logs.Text = await game.ReadLogs();
            }
        }
    }
}