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

namespace GameLibrary.Avalonia.Pages.Library;

public partial class Popup_GameView : UserControl
{
    private int inspectingGameId;
    private TabGroup tabGroup;

    public Popup_GameView()
    {
        InitializeComponent();

        tabGroup = new TabGroup(this);

        btn_Delete.RegisterClick(DeleteGame);
        btn_Overlay.RegisterClick(btn_Overlay_Click);

        btn_Browse.RegisterClick(BrowseToGame);
        btn_Launch.RegisterClick(HandleLaunch);

        ImageManager.RegisterOnGlobalImageChange<ImageBrush>(UpdateGameIcon);
        GameLauncher.OnGameRunStateChange += (a, b) => HelperFunctions.WrapUIThread(() => UpdateRunningGameStatus(a, b)); // need to fix threading issue
    }

    public async Task Draw(dbo_Game game)
    {
        await ValidateGame(game);

        inspectingGameId = game.id;
        img_bg.Background = null;

        UpdateRunningGameStatus(game.id, GameLauncher.IsRunning(game.id));

        await ImageManager.GetGameImage<ImageBrush>(game, UpdateGameIcon);
        await tabGroup.OpenFresh();

        inp_Emulate.SilentSetValue(game.useEmulator);
        lbl_Title.Content = game.gameName;

        List<string> executableBinaries = await GetBinaries(game);
        inp_binary.Setup(executableBinaries.Select(x => Path.GetFileName(x)), executableBinaries.IndexOf(game.executablePath!), HandleBinaryChange);
    }

    private async Task ValidateGame(dbo_Game game)
    {
        bool isDirty = false;

        if (ConfigHandler.isOnLinux)
        {
            if (game.wineProfile == null)
            {
                dbo_WineProfile? firstProfile = await DatabaseHandler.GetItem<dbo_WineProfile>();

                if (firstProfile != null)
                {
                    game.wineProfile = firstProfile.id;
                    isDirty = true;
                }
            }
        }

        if (isDirty)
        {
            await DatabaseHandler.UpdateTableEntry(game, QueryBuilder.SQLEquals(nameof(dbo_WineProfile.id), game.id));
        }
    }

    private void UpdateGameIcon(int gameId, ImageBrush? img)
    {
        if (inspectingGameId != gameId)
            return;

        img_bg.Background = img;
    }

    private async Task<List<string>> GetBinaries(dbo_Game game)
    {
        string gameFolder = await game.GetAbsoluteFolderLocation();

        if (!Directory.Exists(gameFolder))
            return new List<string>();

        return Directory.GetFiles(gameFolder).Where(FilterFile).Select(x => Path.GetFileName(x)).ToList();

        bool FilterFile(string dir)
        {
            return dir.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) ||
                dir.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private void HandleLaunch()
    {
        GameLauncher.LaunchGame(inspectingGameId);
    }

    private void btn_Overlay_Click()
    {
        //GameLauncher.RequestOverlay(inspectingGameId, null);
    }

    private async void BrowseToGame() => await FileManager.BrowseToGame(LibraryHandler.GetGameFromId(inspectingGameId)!);

    private async void HandleBinaryChange()
    {
        await LibraryHandler.ChangeBinaryLocation(inspectingGameId, inp_binary.selectedValue?.ToString());
        //await Draw(LibraryHandler.GetGameFromId(inspectingGameId)!);
    }

    private async void DeleteGame() => await FileManager.DeleteGame(LibraryHandler.GetGameFromId(inspectingGameId)!);


    private void UpdateRunningGameStatus(int gameId, bool to)
    {
        if (gameId != inspectingGameId)
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
            await tabs[activeTab].Open(master.inspectingGameId);
        }




        internal abstract class TabBase
        {
            protected int? lastGameId;
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

            public virtual Task Open(int gameId)
            {
                container!.IsVisible = true;
                lastGameId = gameId;

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
            private HashSet<int>? gameTags;
            private Dictionary<int, Library_Tag> allTags = new Dictionary<int, Library_Tag>();

            public override async Task Open(int gameId)
            {
                if (lastGameId != gameId)
                {
                    await CheckForNewTags();
                    await RedrawSelectedTags();
                }

                await base.Open(gameId);
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

            private async Task RedrawSelectedTags()
            {
                gameTags = (await LibraryHandler.GetGameTags(groupMaster!.master.inspectingGameId)).ToHashSet();

                foreach (KeyValuePair<int, Library_Tag> tag in allTags)
                {
                    tag.Value.Margin = new Thickness(0, 0, 0, 5);
                    tag.Value.Toggle(gameTags.Contains(tag.Key));
                }
            }

            private async void HandleTagToggle(int tagId)
            {
                if (gameTags!.Contains(tagId))
                {
                    gameTags.Remove(tagId);
                    LibraryHandler.RemoveTagFromGame(groupMaster!.master.inspectingGameId, tagId);
                }
                else
                {
                    gameTags.Add(tagId);
                    LibraryHandler.AddTagToGame(groupMaster!.master.inspectingGameId, tagId);
                }

                await RedrawSelectedTags();
            }
        }


        internal class Tab_LaunchSettings : TabBase
        {
            private dbo_WineProfile[]? possibleWineProfiles;

            public override TabBase Setup(Border btn, Grid container, TabGroup groupMaster)
            {
                groupMaster.master.inp_Emulate.RegisterOnChange(HandleEmulateToggle);
                return base.Setup(btn, container, groupMaster);
            }

            public override async Task Open(int gameId)
            {
                if (lastGameId != gameId)
                {
                    possibleWineProfiles = await DatabaseHandler.GetItems<dbo_WineProfile>();
                    dbo_Game game = LibraryHandler.GetGameFromId(gameId)!;

                    if (ConfigHandler.isOnLinux)
                    {
                        groupMaster!.master.inp_WineProfile.IsVisible = true;
                        groupMaster!.master.inp_WineProfile.SetupAsync(possibleWineProfiles!.Select(x => x.profileName), possibleWineProfiles.Select(x => x.id).ToList().IndexOf(game.wineProfile ?? -1), HandleWineProfileChange);
                    }
                    else
                    {
                        groupMaster!.master.inp_WineProfile.IsVisible = false;
                    }
                }


                await base.Open(gameId);
            }

            private void HandleEmulateToggle(bool to) => LibraryHandler.UpdateGameEmulationStatus(lastGameId!.Value, to);
            private async Task HandleWineProfileChange() => await LibraryHandler.ChangeWineProfile(lastGameId!.Value, possibleWineProfiles?.ElementAt(groupMaster!.master.inp_WineProfile.selectedIndex)?.id);
        }

        internal class Tab_Logs : TabBase
        {
            public override TabBase Setup(Border btn, Grid container, TabGroup groupMaster)
            {
                groupMaster.master.btn_RefreshLogs.RegisterClick(async () =>
                {
                    if (lastGameId.HasValue)
                        await RefreshLogs(lastGameId.Value);
                });

                return base.Setup(btn, container, groupMaster);
            }

            public override async Task Open(int gameId)
            {
                await base.Open(gameId);
                await RefreshLogs(gameId);
            }

            private async Task RefreshLogs(int gameId)
            {
                string txt = await GameLauncher.GetLatestLogs(gameId);
                groupMaster!.master.lbl_Logs.Text = txt;
            }
        }
    }
}