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
                TagDto[] newTags = await TagManager.GetAllTags();

                if (allTags.Count == newTags.Length)
                    return;

                allTags.Clear();
                master!.master.cont_AllTags.Children.Clear();

                foreach (TagDto tag in newTags)
                {
                    GenerateTag(tag);
                }

                void GenerateTag(TagDto tag)
                {
                    Library_Tag tagUI = new Library_Tag();

                    if (tag is TagDto_Managed)
                    {
                        tagUI.Draw(tag, null);
                        tagUI.Toggle(true);
                    }
                    else
                    {
                        tagUI.Draw(tag, HandleTagToggle);
                    }

                    master.master.cont_AllTags.Children.Add(tagUI);
                    allTags.Add(tag.id, tagUI);
                }
            }

            private async void HandleTagToggle(TagDto tag)
            {
                if (tag is TagDto_Managed)
                    return;

                await inspectingGame!.ToggleTag(tag.id);
                await RedrawSelectedTags(inspectingGame);
            }

            private async Task RedrawSelectedTags(GameDto game)
            {
                foreach (KeyValuePair<int, Library_Tag> tag in allTags)
                {
                    if (tag.Key < 0)
                        continue;

                    tag.Value.Margin = new Thickness(0, 0, 0, 5);
                    tag.Value.Toggle(game?.tags.Contains(tag.Key) ?? false);
                }
            }
        }

        // Settings

        internal class Tab_LaunchSettings : GameView_TabGroup
        {
            private List<RunnerDto>? possibleRunners;

            public Tab_LaunchSettings(Control element, Common_ButtonToggle btn) : base(element, btn)
            {
            }

            protected override void InternalSetup(GameView_Tabs master)
            {
                master.master.inp_Emulate.RegisterOnChange((b) => UpdateConfigValue(Game_Config.General_LocaleEmulation, b));
                master.master.inp_CaptureLogs.Setup(Enum.GetNames<LoggingLevel>(), 0, async () => await UpdateConfigValue(Game_Config.General_LoggingLevel, master.master.inp_CaptureLogs.selectedIndex));

                master.master.inp_Wine_VirtualDesktop.RegisterOnChange((b) => UpdateConfigValue(Game_Config.Wine_ExplorerLaunch, b));
            }

            protected override async Task OpenWithGame(GameDto? game, bool isNewGame)
            {
                if (isNewGame)
                {
                    DrawRunners(game!);
                    DrawBinaries(game!);
                }

                master!.master.inp_Emulate.SilentSetValue(game!.config.GetBoolean(Game_Config.General_LocaleEmulation, false));
                master.master.inp_CaptureLogs.SilentlyChangeValue(game!.config.GetInteger(Game_Config.General_LoggingLevel, 0));

                master.master.inp_Wine_VirtualDesktop.SilentSetValue(game!.config.GetBoolean(Game_Config.Wine_ExplorerLaunch, false));
            }

            private void DrawRunners(GameDto game)
            {
                possibleRunners = RunnerManager.GetRunnerProfiles().ToList();
                string firstProfile = possibleRunners.Count > 0 ? possibleRunners[0].runnerName : "INVALID";

                string[] profileOptions = [$"Default ({firstProfile})", .. possibleRunners!.Select(x => x.runnerName)!.ToArray()];
                int selectedProfile = possibleRunners.Select(x => x.runnerId).ToList().IndexOf(game.runnerId ?? -1);

                master!.master.inp_WineProfile.IsVisible = true;
                master.master.inp_WineProfile.SetupAsync(profileOptions, selectedProfile >= 0 ? (selectedProfile + 1) : 0, HandleWineProfileChange);
            }

            private void DrawBinaries(GameDto game)
            {
                (int? currentExecutable, string[] possibleBinaries)? options = game.GetPossibleBinaries();

                if (options != null)
                {
                    (master!.master.inp_binary.Parent as Control)!.IsVisible = true;
                    master!.master.inp_binary.SetupAsync(options.Value.possibleBinaries.Select(x => Path.GetFileName(x)), options.Value.currentExecutable, HandleBinaryChange);
                }
                else
                {
                    (master!.master.inp_binary.Parent as Control)!.IsVisible = false;
                }
            }



            private async Task HandleBinaryChange() => await inspectingGame!.ChangeBinaryLocation(master!.master.inp_binary.selectedValue?.ToString());
            private async Task UpdateConfigValue<T>(Game_Config key, T to) => await inspectingGame!.config.SaveGeneric(key, to);

            private async Task HandleWineProfileChange()
            {
                int? newProfileId = null;
                int selectedIndex = master!.master.inp_WineProfile.selectedIndex;

                if (selectedIndex != 0) // default profile
                {
                    newProfileId = possibleRunners![selectedIndex - 1].runnerId;
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