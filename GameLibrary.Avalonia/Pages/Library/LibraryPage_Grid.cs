using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using GameLibrary.Avalonia.Controls;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Pages.Library;

public class LibraryPage_Grid : LibraryPageBase
{
    public static (int x, int y) ContentPerPage = (5, 4);
    public int getTotalContentPerPage => ContentPerPage.x * ContentPerPage.y;

    private int page;

    private Library_Game[] cacheUI;
    private Dictionary<int, int> activeUI;

    public LibraryPage_Grid(Page_Library library) : base(library)
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
        LibraryHandler.RegisterOnGlobalGameChange(RedrawGame);
    }

    public override async Task DrawGames()
    {
        //library.lbl_PageNum.Text = $"{(gamesSlide / getTotalContentPerPage) + 1}";

        int[] games = await LibraryHandler.GetGameList(library.GetGameFilter(page, ContentPerPage.x * ContentPerPage.y));
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

    public async Task RedrawGame(int gameId)
    {
        if (activeUI.TryGetValue(gameId, out int uiPos))
            await cacheUI[uiPos].RedrawGameDetails(gameId, false);
    }

    public void UpdateImage(int gameId, ImageBrush? brush)
    {
        if (activeUI.TryGetValue(gameId, out int uiPos))
            cacheUI[uiPos].RedrawIcon(gameId, brush);
    }

    public override async Task PrevPage()
    {
        page--;
        await DrawGames();
    }

    public override async Task NextPage()
    {
        page++;
        await DrawGames();
    }

    public override async Task FirstPage()
    {
        page = 0;
        await DrawGames();
    }

    public override async Task LastPage()
    {
        // ? need to get count on init
        await DrawGames();
    }
}
