using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GameLibrary.AvaloniaUI.Controls.Pages.Library;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.AvaloniaUI.Controls.Pages;

public partial class Page_Library : UserControl
{
    private HashSet<TagDto> activeTags = new HashSet<TagDto>();
    private Dictionary<TagDto, Library_Tag> generatedTagUI = new Dictionary<TagDto, Library_Tag>();

    private bool currentSortAscending = true;
    private GameFilterRequest.OrderType currentSort = GameFilterRequest.OrderType.Id;

    private LibraryPageBase? gameList;
    private bool disableBackgroundImages;

    private Dictionary<string, Library_ActiveProcess> activeGameUI = new Dictionary<string, Library_ActiveProcess>();

    public Page_Library()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        BindButtons();
        ToggleMenu(false);

        DrawEverything();
        CreateGameList();

        LibraryManager.onGameDetailsUpdate += async (int gameId) => await RedrawGameFromGameList(gameId);
        LibraryManager.onGameDeletion += async () => await RedrawGameList();

        RunnerManager.onGameStatusChange += UpdateActiveGameList;
    }

    private async void DrawEverything()
    {
        RedrawSortNames();
        await DrawTags();
    }

    private async void CreateGameList()
    {
        gameList = null;
        disableBackgroundImages = ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Appearance_BackgroundImage, false);

        switch (ConfigHandler.configProvider!.GetInteger(ConfigKeys.Appearance_Layout, 0))
        {
            case 1:
                gameList = new LibraryPage_Endless(this);
                break;

            default:
                gameList = new LibraryPage_Grid(this);
                break;
        }

        cont_GameList.Children.Add(gameList);

        await gameList!.DrawGames();
    }


    private void BindButtons()
    {
        Indexer.Setup(RedrawGameList);

        GameViewer.PointerPressed += (_, e) => e.Handled = true;
        Indexer.PointerPressed += (_, e) => e.Handled = true;
        Settings.PointerPressed += (_, e) => e.Handled = true;

        cont_MenuView.PointerPressed += (_, __) => ToggleMenu(false);

        btn_Tags.RegisterClick(OpenTagManager);
        btn_Indexer.RegisterClick(OpenIndexer);
        btn_Settings.RegisterClick(OpenSettings);

        inp_Search.OnChange(RedrawGameList);
        btn_SortDir.RegisterClick(UpdateSortDirection);

        btn_SorType.Setup(GameFilterRequest.getOrderOptionsNames, 0, UpdateSortType);

        (int type, bool asc) = GameFilterRequest.DecodeOrder(ConfigHandler.configProvider!.GetInteger(ConfigKeys.Appearance_DefaultFilter, 0));

        btn_SorType.ChangeValue(type);
        currentSortAscending = asc;

        RedrawSortNames();
    }

    public async Task DrawTags()
    {
        generatedTagUI.Clear();
        activeTags.Clear();

        cont_AllTags.Children.Clear();

        TagDto[] tags = await TagManager.GetAllTags();

        foreach (TagDto tag in tags)
        {
            if (generatedTagUI.ContainsKey(tag))
                continue;

            generatedTagUI.Add(tag, CreateTagUI(tag));
        }

        Library_Tag CreateTagUI(TagDto tag)
        {
            Library_Tag ui = new Library_Tag();
            ui.Draw(tag, SwapTagMode);

            cont_AllTags.Children.Add(ui);
            return ui;
        }
    }

    private async void SwapTagMode(TagDto tagId)
    {
        if (generatedTagUI.TryGetValue(tagId, out Library_Tag? ui))
        {
            if (activeTags.Contains(tagId))
            {
                activeTags.Remove(tagId);
                ui.Toggle(false);
            }
            else
            {
                activeTags.Add(tagId);
                ui.Toggle(true);
            }
        }
    }

    public void ToggleGameView(int gameId)
    {
        GameDto? game = LibraryManager.TryGetCachedGame(gameId);

        if (game == null)
        {
            ToggleMenu(false);
            return;
        }

        ToggleMenu(true);
        GameViewer.IsVisible = true;

        GameViewer.Draw(game);
    }

    private async Task RedrawGameList() => await RedrawGameList(false);
    private async Task RedrawGameList(bool resetLayout)
    {
        if (gameList == null)
            return;

        if (resetLayout)
            gameList!.ResetLayout();

        await gameList!.DrawGames();
    }

    private async Task RedrawGameFromGameList(int gameId)
    {
        if (gameList == null)
            return;

        await gameList.RefreshGame(gameId);
    }



    public void ToggleMenu(bool to)
    {
        eff_Blur.Effect = to ? new ImmutableBlurEffect(20) : null;
        cont_MenuView.IsVisible = to;

        GameViewer.IsVisible = false;
        Settings.IsVisible = false;
        Indexer.IsVisible = false;
        TagEditor.IsVisible = false;
    }

    private void OpenIndexer()
    {
        ToggleMenu(true);
        Indexer.IsVisible = false;
        Indexer.OnOpen();
    }
    private async Task OpenSettings()
    {
        ToggleMenu(true);
        Settings.IsVisible = true;
        await Settings.OnOpen();
    }

    private void OpenTagManager()
    {
        ToggleMenu(true);
        TagEditor.IsVisible = true;
        TagEditor.OnOpen();
    }



    private void UpdateActiveGameList(string path, bool status)
    {
        Dispatcher.UIThread.Post(Internal);

        void Internal()
        {
            Library_ActiveProcess ui;

            if (activeGameUI.TryGetValue(path, out ui!))
            {
                if (!status)
                {
                    cont_ActiveGames.Children.Remove(ui);
                    activeGameUI.Remove(path);
                }

                return;
            }

            ui = new Library_ActiveProcess();
            ui.Draw(path);

            cont_ActiveGames.Children.Add(ui);
            activeGameUI[path] = ui;
        }
    }


    // sort
    private async void UpdateSortDirection()
    {
        currentSortAscending = !currentSortAscending;
        RedrawSortNames();

        await RedrawGameList(true);
    }

    private async void UpdateSortType()
    {
        currentSort = (GameFilterRequest.OrderType)btn_SorType.selectedIndex;
        await RedrawGameList(true);
    }

    private void RedrawSortNames()
    {
        btn_SortDir.Label = currentSortAscending ? "Ascending" : "Descending";
    }

    public GameFilterRequest GetGameFilter(int page, int contentPerPage)
    {
        return new GameFilterRequest()
        {
            nameFilter = inp_Search.getText,
            tagList = activeTags,
            orderDirection = currentSortAscending,
            orderType = currentSort,
            page = page,
            contentPerPage = contentPerPage
        };
    }

    public void UpdateBackgroundImage(ImageBrush? brush)
    {
        if (disableBackgroundImages || brush == null)
            return;

        img_Bg.Source = (IImage)brush!.Source!;
    }
}