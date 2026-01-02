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
using GameLibrary.Avalonia.Controls.Pages.Library;
using GameLibrary.Avalonia.Utils;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Avalonia.Controls.SubPage;

public partial class Popup_GameView : UserControl
{
    private GameDto? inspectingGame;
    private TabGroup tabGroup; // change this to be based on the generic one i made

    public Popup_GameView()
    {
        InitializeComponent();

        tabGroup = new TabGroup(this);

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

        await ImageManager.GetGameImage<ImageBrush>(game, UpdateGameIcon);
        await tabGroup.OpenFresh();

        lbl_Title.Content = game.getGame.gameName;

        (int? currentExecutable, string[] possibleBinaries) = game.GetPossibleBinaries();
        inp_binary.SetupAsync(possibleBinaries.Select(x => Path.GetFileName(x)), currentExecutable, HandleBinaryChange);
    }

    private void UpdateGameIcon(int gameId, ImageBrush? img)
    {
        if (inspectingGame?.getGameId != gameId)
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

    private async Task OpenOverlay() => await OverlayManager.LaunchOverlay(inspectingGame!.getGameId);
    private async Task HandleBinaryChange() => await inspectingGame!.ChangeBinaryLocation(inp_binary.selectedValue?.ToString());

    private async Task StartNameChange()
    {
        string? res = await DependencyManager.uiLinker!.OpenStringInputModal("Game Name", inspectingGame!.getGame.gameName);

        if (!string.IsNullOrEmpty(res))
            await inspectingGame!.UpdateGameName(res);
    }

    private async Task RefreshSelectedGame(int gameId)
    {
        if (gameId != inspectingGame?.getGameId)
            return;

        await Draw(inspectingGame!);
    }

    private void UpdateRunningGameStatus(string binary, bool to)
    {
        if (inspectingGame!.getAbsoluteBinaryLocation != binary)
            return;

        btn_Launch.Label = to ? "Stop" : "Play";
    }

    private class TabGroup
    {
        private Popup_GameView master;

        private int activeTab = 0;
        private TabBase[] tabs;

        private ImmutableSolidColorBrush activeTabColour;
        private ImmutableSolidColorBrush unselectedTabColour;

        public TabGroup(Popup_GameView master)
        {
            this.master = master;
            activeTabColour = new ImmutableSolidColorBrush(Color.FromRgb(18, 18, 18));
            unselectedTabColour = new ImmutableSolidColorBrush(Color.FromRgb(25, 25, 25));

            tabs = [
                CreateTab<Tab_Tags>(0, master.btn_tab_Tags, master.tab_Tags),
                CreateTab<Tab_LaunchSettings>(1, master.btn_tab_Options, master.tab_Settings),
                CreateTab<Tab_Logs>(2, master.btn_tab_Logs, master.tab_Logs)
            ];

            activeTab = 0;
        }

        internal TabBase CreateTab<T>(int pos, Border btn, Grid container) where T : TabBase
        {
            btn.PointerPressed += async (_, __) => await SwitchTab(pos);
            TabBase tab = Activator.CreateInstance<T>();

            return tab.Setup(btn, container, this);
        }

        public async Task OpenFresh()
        {
            await SwitchTab(activeTab);
        }

        public async Task SwitchTab(int to)
        {
            if (activeTab != to)
                tabs[activeTab].Close();

            activeTab = to;
            await tabs[activeTab].Open(master.inspectingGame);
        }




        internal abstract class TabBase
        {
            protected GameDto? lastGame;
            protected TabGroup? groupMaster;

            private Grid? container;
            private Border? btn;

            public virtual TabBase Setup(Border btn, Grid container, TabGroup groupMaster)
            {
                this.groupMaster = groupMaster;
                this.container = container;
                this.btn = btn;

                container.IsVisible = false;
                Close();

                return this;
            }

            public virtual Task Open(GameDto? game)
            {
                container!.IsVisible = true;
                lastGame = game;

                btn!.Background = groupMaster!.activeTabColour;

                return Task.CompletedTask;
            }

            public virtual void Close()
            {
                container!.IsVisible = false;
                btn!.Background = groupMaster!.unselectedTabColour;
            }
        }




        internal class Tab_Tags : TabBase
        {
            private Dictionary<int, Library_Tag> allTags = new Dictionary<int, Library_Tag>();

            public override async Task Open(GameDto? game)
            {
                if (lastGame != game)
                {
                    await CheckForNewTags();
                    await RedrawSelectedTags(game!);
                }

                await base.Open(game);
            }

            public async Task CheckForNewTags()
            {
                int[] newTags = await LibraryHandler.GetAllTags();

                if (allTags.Count == newTags.Length)
                    return;

                allTags.Clear();
                groupMaster!.master.cont_AllTags.Children.Clear();

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

                        groupMaster!.master.cont_AllTags.Children.Add(tagUI);
                        allTags.Add(tagId, tagUI);
                    }
                }
            }

            private async void HandleTagToggle(int tagId)
            {
                await groupMaster!.master.inspectingGame!.ToggleTag(tagId);
                await RedrawSelectedTags(groupMaster!.master.inspectingGame);
            }

            private async Task RedrawSelectedTags(GameDto game)
            {
                foreach (KeyValuePair<int, Library_Tag> tag in allTags)
                {
                    tag.Value.Margin = new Thickness(0, 0, 0, 5);
                    tag.Value.Toggle(game?.getTags.Contains(tag.Key) ?? false);
                }
            }
        }


        internal class Tab_LaunchSettings : TabBase
        {
            private List<(int id, string name)>? possibleRunners;

            public override TabBase Setup(Border btn, Grid container, TabGroup groupMaster)
            {
                groupMaster.master.inp_Emulate.RegisterOnChange(HandleEmulateToggle);
                groupMaster.master.inp_CaptureLogs.RegisterOnChange(HandleCaptureLogs);

                return base.Setup(btn, container, groupMaster);
            }

            public override async Task Open(GameDto? game)
            {
                if (lastGame != game)
                {
                    possibleRunners = await RunnerManager.GetRunnerProfiles();// await DatabaseHandler.GetItems<dbo_WineProfile>(QueryBuilder.OrderBy(nameof(dbo_WineProfile.isDefault), true));
                    string firstProfile = possibleRunners.Count > 0 ? possibleRunners[0].name : "INVALID";

                    string[] profileOptions = [$"Default ({firstProfile})", .. possibleRunners!.Select(x => x.name)!.ToArray()];
                    int selectedProfile = possibleRunners.Select(x => x.id).ToList().IndexOf(game?.getGame.runnerId ?? -1);

                    groupMaster!.master.inp_WineProfile.IsVisible = true;
                    groupMaster!.master.inp_WineProfile.SetupAsync(profileOptions, selectedProfile >= 0 ? (selectedProfile + 1) : 0, HandleWineProfileChange);

                }

                groupMaster!.master.inp_Emulate.SilentSetValue(game!.useRegionEmulation);
                groupMaster!.master.inp_CaptureLogs.SilentSetValue(game!.captureLogs);

                await base.Open(game);
            }

            private async Task HandleEmulateToggle(bool to) => await lastGame!.UpdateGameEmulationStatus(to);
            private async Task HandleCaptureLogs(bool to) => await lastGame!.UpdateCaptureLogsStatus(to);

            private async Task HandleWineProfileChange()
            {
                int? newProfileId = null;
                int selectedIndex = groupMaster!.master.inp_WineProfile.selectedIndex;

                if (selectedIndex != 0) // default profile
                {
                    newProfileId = possibleRunners![selectedIndex - 1].id;
                }

                await lastGame!.ChangeRunnerId(newProfileId);
            }
        }

        internal class Tab_Logs : TabBase
        {
            public override TabBase Setup(Border btn, Grid container, TabGroup groupMaster)
            {
                return base.Setup(btn, container, groupMaster);
            }

            public override async Task Open(GameDto? game)
            {
                await base.Open(game);
                await RefreshLogs(game);
            }

            private async Task RefreshLogs(GameDto? game)
            {
                if (game == null)
                {
                    groupMaster!.master.lbl_Logs.Text = "";
                    return;
                }

                groupMaster!.master.lbl_Logs.Text = await game.ReadLogs();
            }
        }
    }
}