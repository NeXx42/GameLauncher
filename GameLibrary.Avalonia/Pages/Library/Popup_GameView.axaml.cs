using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GameLibrary.Avalonia.Controls;
using GameLibrary.Avalonia.Utils;
using GameLibrary.DB;
using GameLibrary.DB.Database.Tables;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Avalonia.Pages.Library;

public partial class Popup_GameView : UserControl
{
    private GameDto inspectingGame;
    private TabGroup tabGroup;

    public Popup_GameView()
    {
        InitializeComponent();

        tabGroup = new TabGroup(this);

        btn_Delete.RegisterClick(DeleteGame);
        btn_Overlay.RegisterClick(OpenOverlay);

        btn_Browse.RegisterClick(BrowseToGame);
        btn_Launch.RegisterClick(HandleLaunch);

        lbl_Title.PointerPressed += async (_, __) => await StartNameChange();

        ImageManager.RegisterOnGlobalImageChange<ImageBrush>(UpdateGameIcon);
        LibraryHandler.RegisterOnGlobalGameChange(RefreshSelectedGame);

        GameLauncher.OnGameRunStateChange += (a, b) => HelperFunctions.WrapUIThread(() => UpdateRunningGameStatus(a, b)); // need to fix threading issue
    }

    public async Task Draw(GameDto game)
    {
        inspectingGame = game;
        img_bg.Background = null;

        UpdateRunningGameStatus(game.getGameId, GameLauncher.IsRunning(game.getGameId));

        await ImageManager.GetGameImage<ImageBrush>(game, UpdateGameIcon);
        await tabGroup.OpenFresh();

        lbl_Title.Content = game.getGame.gameName;

        (int? currentExecutable, string[] possibleBinaries) = game.GetPossibleBinaries();
        inp_binary.Setup(possibleBinaries.Select(x => Path.GetFileName(x)), currentExecutable, HandleBinaryChange);
    }

    private void UpdateGameIcon(int gameId, ImageBrush? img)
    {
        if (inspectingGame?.getGameId != gameId)
            return;

        img_bg.Background = img;
    }


    private void HandleLaunch()
    {
        inspectingGame.Launch();
    }

    private void BrowseToGame() => inspectingGame?.BrowseToGame();
    private async Task OpenOverlay() => await OverlayManager.LaunchOverlay(inspectingGame!.getGameId);

    private async void HandleBinaryChange() => await inspectingGame!.ChangeBinaryLocation(inp_binary.selectedValue?.ToString());
    private async Task DeleteGame() => await FileManager.StartDeletion(inspectingGame);

    private async Task StartNameChange()
    {
        string? res = await DependencyManager.uiLinker!.OpenStringInputModal("Game Name");

        if (!string.IsNullOrEmpty(res))
            await inspectingGame!.UpdateGameName(res);
    }

    private async Task RefreshSelectedGame(int gameId)
    {
        if (gameId != inspectingGame?.getGameId)
            return;

        await Draw(inspectingGame!);
    }

    private void UpdateRunningGameStatus(int gameId, bool to)
    {
        if (gameId != inspectingGame.getGameId)
            return;

        lbl_IsRunning.IsVisible = to;
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
                await groupMaster!.master.inspectingGame.ToggleTag(tagId);
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
            private dbo_WineProfile[]? possibleWineProfiles;

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
                    possibleWineProfiles = await DatabaseHandler.GetItems<dbo_WineProfile>(QueryBuilder.OrderBy(nameof(dbo_WineProfile.isDefault), true));

                    if (ConfigHandler.isOnLinux)
                    {
                        string defaultOption = possibleWineProfiles.FirstOrDefault(x => x.isDefault)?.profileName ?? "INVALID";
                        int indexOfSelectedWineProfile = possibleWineProfiles.Select(x => x.id).ToList().IndexOf(game!.getWineProfile?.id ?? -1);

                        string[] profileOptions = [$"Default ({defaultOption})", .. possibleWineProfiles!.Select(x => x.profileName)!];

                        groupMaster!.master.inp_WineProfile.IsVisible = true;
                        groupMaster!.master.inp_WineProfile.SetupAsync(profileOptions, indexOfSelectedWineProfile < 0 ? 0 : indexOfSelectedWineProfile, HandleWineProfileChange);
                    }
                    else
                    {
                        groupMaster!.master.inp_WineProfile.IsVisible = false;
                    }
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
                    newProfileId = possibleWineProfiles!.ElementAt(selectedIndex - 1)?.id;
                }

                await lastGame!.ChangeWineProfile(newProfileId);
            }
        }

        internal class Tab_Logs : TabBase
        {
            public override TabBase Setup(Border btn, Grid container, TabGroup groupMaster)
            {
                groupMaster.master.btn_RefreshLogs.RegisterClick(async () =>
                {
                    if (lastGame != null)
                        await RefreshLogs(lastGame);
                });

                return base.Setup(btn, container, groupMaster);
            }

            public override async Task Open(GameDto? game)
            {
                await base.Open(game);
                await RefreshLogs(game);
            }

            private async Task RefreshLogs(GameDto? game)
            {
                string txt = await GameLauncher.GetLatestLogs(game!.getGameId);
                groupMaster!.master.lbl_Logs.Text = txt;
            }
        }
    }
}