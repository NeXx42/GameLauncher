using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using GameLibrary.AvaloniaUI.Controls.Pages.Library;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.Pages;

public partial class Page_Library : UserControl
{
    private HashSet<int> activeTags = new HashSet<int>();
    private Dictionary<int, Library_Tag> existingTags = new Dictionary<int, Library_Tag>();

    private bool currentSortAscending = true;
    private GameFilterRequest.OrderType currentSort = GameFilterRequest.OrderType.Id;
    private Dictionary<GameFilterRequest.OrderType, Common_ButtonToggle> sortBtns = new Dictionary<GameFilterRequest.OrderType, Common_ButtonToggle>();

    private LibraryPageBase gameList;


    public Page_Library()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        gameList = new LibraryPage_Grid(this);

        BindButtons();
        ToggleMenu(false);

        DrawEverything();

        LibraryHandler.onGameDetailsUpdate += async (int gameId) => await gameList.RefreshGame(gameId);
        LibraryHandler.onGameDeletion += async () => await gameList.DrawGames();
    }

    private async void DrawEverything()
    {
        ToggleTagCreator(false);
        RedrawSortNames();

        await gameList.DrawGames();
        await DrawTags();
    }

    private void BindButtons()
    {
        Indexer.Setup(() => gameList.DrawGames());

        GameViewer.PointerPressed += (_, e) => e.Handled = true;
        Indexer.PointerPressed += (_, e) => e.Handled = true;
        Settings.PointerPressed += (_, e) => e.Handled = true;

        cont_MenuView.PointerPressed += (_, __) => ToggleMenu(false);

        //btn_OpenTagCreator.RegisterClick(() => ToggleTagCreator(null));
        //btn_CreateTag.RegisterClick(CreateTag);

        btn_FirstPage.RegisterClick(gameList.FirstPage);
        btn_PrevPage.RegisterClick(gameList.PrevPage);
        btn_NextPage.RegisterClick(gameList.NextPage);
        btn_LastPage.RegisterClick(gameList.LastPage);

        btn_Indexer.RegisterClick(OpenIndexer);
        btn_Settings.RegisterClick(OpenSettings);

        inp_Search.KeyUp += (_, __) => gameList.DrawGames();
        btn_SortDir.RegisterClick(UpdateSortDirection);

        foreach (GameFilterRequest.OrderType type in Enum.GetValues(typeof(GameFilterRequest.OrderType)))
        {
            Common_ButtonToggle btn = new Common_ButtonToggle();
            btn.Label = type.ToString();
            btn.MinWidth = 40;

            btn.Register(_ => UpdateSortType(type));

            sortBtns.Add(type, btn);
            inp_SortTypes.Children.Add(btn);
        }

        UpdateSortType(GameFilterRequest.OrderType.Id);
    }

    public async Task DrawTags()
    {
        existingTags.Clear();
        activeTags.Clear();

        cont_AllTags.Children.Clear();

        int[] tagIds = await LibraryHandler.GetAllTags();

        foreach (int tagId in tagIds)
        {
            if (existingTags.ContainsKey(tagId))
                continue;

            existingTags.Add(tagId, CreateTagUI(tagId));
        }

        Library_Tag CreateTagUI(int tagId)
        {
            dbo_Tag? tag = LibraryHandler.GetTagById(tagId);

            Library_Tag ui = new Library_Tag();
            ui.Draw(tag!, SwapTagMode);

            cont_AllTags.Children.Add(ui);
            return ui;
        }
    }

    private async void SwapTagMode(int tagId)
    {
        if (existingTags.TryGetValue(tagId, out Library_Tag? ui))
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

        await gameList.DrawGames();
    }

    private async void CreateTag()
    {
        //string newTagName = inp_TagName.Text!;
        //
        //if (string.IsNullOrEmpty(newTagName))
        //    return;
        //
        //inp_TagName.Text = "";
        //
        //await LibraryHandler.CreateTag(newTagName);
        //
        //LibraryHandler.MarkTagsAsDirty();
        //await DrawTags();
    }

    public async void ToggleGameView(int? gameId)
    {
        if (gameId == null)
        {
            ToggleMenu(false);
            return;
        }

        GameDto? game = LibraryHandler.TryGetCachedGame(gameId.Value);

        if (game == null)
        {
            ToggleMenu(false);
            return;
        }

        ToggleMenu(true);
        GameViewer.IsVisible = true;

        await GameViewer.Draw(game);
    }

    public void ToggleMenu(bool to)
    {
        eff_Blur.Effect = to ? new ImmutableBlurEffect(20) : null;//. .Radius = to ? 10 : 0;
        cont_MenuView.IsVisible = to;

        GameViewer.IsVisible = false;
        Settings.IsVisible = false;
        Indexer.IsVisible = false;
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

    private void ToggleTagCreator(bool? to)
    {
        //if (!to.HasValue)
        //{
        //    to = !cont_TagCreator.IsVisible;
        //}
        //
        //cont_TagCreator.IsVisible = to.Value;
        //btn_OpenTagCreator.Label = to.Value ? $"Close" : "+ Add Tag";
    }

    // sort
    private async void UpdateSortDirection()
    {
        currentSortAscending = !currentSortAscending;
        RedrawSortNames();

        await gameList.DrawGames();
    }

    private async void UpdateSortType(GameFilterRequest.OrderType to)
    {
        currentSort = to;

        foreach (var order in sortBtns)
            order.Value.isSelected = order.Key == to;

        await gameList.DrawGames();
    }

    private void RedrawSortNames()
    {
        btn_SortDir.Label = currentSortAscending ? "Ascending" : "Descending";
    }

    public GameFilterRequest GetGameFilter(int page, int contentPerPage)
    {
        return new GameFilterRequest()
        {
            nameFilter = inp_Search.Text,
            tagList = activeTags,
            orderDirection = currentSortAscending,
            orderType = currentSort,
            page = page,
            contentPerPage = contentPerPage
        };
    }
}