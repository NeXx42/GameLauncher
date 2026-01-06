using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using GameLibrary.AvaloniaUI.Controls;
using GameLibrary.Logic;
using ZstdSharp.Unsafe;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public class LibraryPage_Grid : LibraryPageBase
{
    public static (int x, int y) ContentPerPage = (6, 3);
    public static (int width, int height) CardSize = (290, 257);

    public int getTotalContentPerPage => ContentPerPage.x * ContentPerPage.y;

    private int page;
    private int? hoveredGame
    {
        get => m_HoveredGame;
        set
        {
            if (m_HoveredGame.HasValue)
            {
                cacheUI[m_HoveredGame.Value].ToggleHover(false);
            }

            m_HoveredGame = value;

            if (m_HoveredGame.HasValue)
            {
                library.UpdateBackgroundImage(cacheUI[m_HoveredGame.Value].getImage);
                cacheUI[m_HoveredGame.Value].ToggleHover(true);
            }
            else
            {
                library.UpdateBackgroundImage(null);
            }
        }
    }
    private int? m_HoveredGame;

    private Library_Game[] cacheUI;
    private Dictionary<int, int> activeUI;

    public LibraryPage_Grid(Page_Library library) : base(library)
    {
        this.library = library;

        cacheUI = new Library_Game[getTotalContentPerPage];
        activeUI = new Dictionary<int, int>();

        library.cont_Games.Children.Clear();

        for (int i = 0; i < cacheUI.Length; i++)
        {
            Library_Game ui = new Library_Game();
            ui.Width = CardSize.width;
            ui.Height = CardSize.height; // + padding + title
            ui.Margin = new Thickness(0, 0, 2, 0);

            library.cont_Games.Children.Add(ui);
            cacheUI[i] = ui;

            int temp = i;
            ui.pointerStatusChange += (enter) => hoveredGame = (enter ? temp : null);
        }
    }

    public override async Task DrawGames()
    {
        library.ToggleMenu(false);

        activeUI.Clear();
        library.lbl_PageNum.Text = $"{page + 1}";

        foreach (Library_Game ui in cacheUI)
        {
            ui.IsVisible = true;
            ui.DrawSkeleton();
        }

        int[] games = await LibraryManager.GetGameList(library.GetGameFilter(page, ContentPerPage.x * ContentPerPage.y));

        for (int i = 0; i < cacheUI.Length; i++)
        {
            if (i >= games.Length)
            {
                cacheUI[i].IsVisible = false;
                continue;
            }

            cacheUI[i].IsVisible = true;
            cacheUI[i].ToggleHover(false);

            await cacheUI[i].Draw(games[i], ViewGame);

            activeUI.Add(games[i], i);
        }

        hoveredGame = null;
    }

    private void ViewGame(int? id)
    {
        if (id == null)
            return;

        hoveredGame = activeUI[id.Value];
        library.ToggleGameView(id.Value);
    }

    public override async Task RefreshGame(int gameId)
    {
        if (activeUI.TryGetValue(gameId, out int uiPos))
            await cacheUI[uiPos].RedrawGameDetails(gameId);
    }

    public void UpdateImage(int gameId, ImageBrush? brush)
    {
        if (activeUI.TryGetValue(gameId, out int uiPos))
            cacheUI[uiPos].RedrawIcon(gameId, brush);
    }

    public override async Task PrevPage() => await UpdatePage(Math.Max(page - 1, 0));
    public override async Task NextPage() => await UpdatePage(Math.Min(page + 1, LibraryManager.GetMaxPages(ContentPerPage.x * ContentPerPage.y)));
    public override async Task FirstPage() => await UpdatePage(0);
    public override async Task LastPage() => await UpdatePage(LibraryManager.GetMaxPages(ContentPerPage.x * ContentPerPage.y));

    private async Task UpdatePage(int to)
    {
        if (page == to)
            return;

        page = to;
        await DrawGames();
    }
}
