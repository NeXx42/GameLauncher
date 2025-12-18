using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.Avalonia.Controls;
using GameLibrary.Avalonia.Pages.Library;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Avalonia.Pages;

public partial class Page_Library : UserControl
{
    private HashSet<int> activeTags = new HashSet<int>();
    private Dictionary<int, Library_Tag> existingTags = new Dictionary<int, Library_Tag>();

    private bool currentSortAscending = true;
    private LibraryHandler.OrderType currentSort = LibraryHandler.OrderType.Id;

    private GameList gameList;


    public Page_Library()
    {
        InitializeComponent();
        gameList = new GameList(this);

        BindButtons();
        ToggleMenu(false);

        DrawEverything();
    }

    private async void DrawEverything()
    {
        ToggleTagCreator(false);

        await gameList.DrawGames();
        await DrawTags();

        RedrawSortNames();
    }

    private void BindButtons()
    {
        Indexer.Setup(() => gameList.RefilterGames());

        GameViewer.PointerPressed += (_, e) => e.Handled = true;
        Indexer.PointerPressed += (_, e) => e.Handled = true;
        Settings.PointerPressed += (_, e) => e.Handled = true;

        cont_MenuView.PointerPressed += (_, __) => ToggleMenu(false);

        btn_OpenTagCreator.RegisterClick(() => ToggleTagCreator(null));
        btn_CreateTag.RegisterClick(CreateTag);

        btn_FirstPage.RegisterClick(gameList.FirstPage);
        btn_PrevPage.RegisterClick(gameList.PrePage);
        btn_NextPage.RegisterClick(gameList.NextPage);
        btn_LastPage.RegisterClick(gameList.LastPage);

        btn_Indexer.RegisterClick(OpenIndexer);
        btn_Settings.RegisterClick(OpenSettings);

        btn_SortDir.RegisterClick(UpdateSortDirection);
        inp_SortType.Setup(Enum.GetValues(typeof(LibraryHandler.OrderType)), 0, UpdateSortType);
    }

    private async Task RedrawGameImages()
    {

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

        await gameList.RefilterGames();
    }

    private async void CreateTag()
    {
        string newTagName = inp_TagName.Text!;

        if (string.IsNullOrEmpty(newTagName))
            return;

        inp_TagName.Text = "";

        await DatabaseHandler.InsertIntoTable(new dbo_Tag()
        {
            TagName = newTagName,
        });

        LibraryHandler.MarkTagsAsDirty();
        await DrawTags();
    }




    private async void ToggleGameView(int gameId)
    {
        GameDto? game = LibraryHandler.GetGameFromId(gameId);

        if (game != null)
        {
            await GameViewer.Draw(game);
        }

        ToggleMenu(true);
        GameViewer.IsVisible = true;
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
        if (!to.HasValue)
        {
            to = !cont_TagCreator.IsVisible;
        }

        cont_TagCreator.IsVisible = to.Value;
        btn_OpenTagCreator.Label = to.Value ? $"Close" : "+ Add Tag";
    }


    // sort
    private async void UpdateSortDirection()
    {
        currentSortAscending = !currentSortAscending;
        RedrawSortNames();

        await gameList.RefilterGames();
    }

    private async void UpdateSortType()
    {
        currentSort = (LibraryHandler.OrderType)inp_SortType.selectedIndex;
        await gameList.RefilterGames();
    }

    private void RedrawSortNames()
    {
        btn_SortDir.Label = currentSortAscending ? "Ascending" : "Descending";
    }


    // page controls





    private class GameList
    {
        public static (int x, int y) ContentPerPage = (5, 4);
        public int getTotalContentPerPage => ContentPerPage.x * ContentPerPage.y;

        private int gamesSlide;

        private Library_Game[] cacheUI;
        private Dictionary<int, int> activeUI;

        private Page_Library library;


        public GameList(Page_Library library)
        {
            this.library = library;
            const float ratio = 0.1927083333f;

            cacheUI = new Library_Game[getTotalContentPerPage];
            activeUI = new Dictionary<int, int>();

            library.cont_Games.Children.Clear();

            for (int i = 0; i < cacheUI.Length; i++)
            {
                Library_Game ui = new Library_Game();
                ui.Width = 1920 * ratio;
                ui.Height = 1080 * ratio; // + padding + title
                ui.Margin = new Thickness(0, 0, 2, 0);

                library.cont_Games.Children.Add(ui);
                cacheUI[i] = ui;
            }

            ImageManager.RegisterOnGlobalImageChange<ImageBrush>(UpdateImage);
        }

        public async Task RefilterGames(bool resetPage = true)
        {
            if (resetPage)
                gamesSlide = 0;

            LibraryHandler.RefilterGames(library.activeTags, library.currentSort, library.currentSortAscending);
            await DrawGames();
        }

        public async Task DrawGames()
        {
            library.lbl_PageNum.Text = $"{(gamesSlide / getTotalContentPerPage) + 1}";

            int[] games = LibraryHandler.GetDrawList(gamesSlide, getTotalContentPerPage);
            activeUI.Clear();

            for (int i = 0; i < cacheUI.Length; i++)
            {
                if (i >= games.Length)
                {
                    cacheUI[i].IsVisible = false;
                    continue;
                }

                cacheUI[i].IsVisible = true;
                await cacheUI[i].Draw(games[i], library.ToggleGameView);

                activeUI.Add(games[i], i);
            }
        }

        public async void UpdateImage(int gameId, ImageBrush? brush)
        {
            if (activeUI.TryGetValue(gameId, out int uiPos))
                cacheUI[uiPos].RedrawIcon(gameId, brush);
        }

        public async Task PrePage()
        {
            gamesSlide -= getTotalContentPerPage;
            gamesSlide = Math.Max(0, gamesSlide);

            await DrawGames();
        }

        public async Task NextPage()
        {
            int maxSlide = ((int)Math.Floor(LibraryHandler.GetFilteredGameCount() / (float)getTotalContentPerPage)) * getTotalContentPerPage;

            gamesSlide += (int)getTotalContentPerPage;
            gamesSlide = Math.Min(gamesSlide, maxSlide);

            await DrawGames();
        }

        public async Task FirstPage()
        {
            gamesSlide = 0;
            await DrawGames();
        }

        public async Task LastPage()
        {
            gamesSlide = ((int)Math.Floor(LibraryHandler.GetFilteredGameCount() / (float)getTotalContentPerPage)) * getTotalContentPerPage;
            await DrawGames();
        }
    }
}