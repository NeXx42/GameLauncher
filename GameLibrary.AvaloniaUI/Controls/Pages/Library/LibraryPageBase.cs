using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public abstract class LibraryPageBase : UserControl
{
    public static (int width, int height) CardSize = (290, 257);

    protected int? hoveredGame
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

    protected Page_Library library;
    protected List<Library_Game> cacheUI;
    protected Dictionary<int, int> activeUI;

    public abstract Panel getGameChild { get; }


    public LibraryPageBase(Page_Library library)
    {
        this.library = library;

        cacheUI = new List<Library_Game>();
        activeUI = new Dictionary<int, int>();
    }


    public virtual async Task DrawGames()
    {
        library.ToggleMenu(false);

        foreach (Library_Game ui in cacheUI)
        {
            ui.IsVisible = true;
            ui.DrawSkeleton();
        }

        await Dispatcher.UIThread.InvokeAsync(() => { });
        int[] games = await LibraryManager.GetGameList(GetGameFilter());

        activeUI.Clear();

        for (int i = 0; i < Math.Max(cacheUI.Count, games.Length); i++)
        {
            if (i >= games.Length)
            {
                cacheUI[i].IsVisible = false;
                continue;
            }
            else if (i >= cacheUI.Count)
            {
                CreateGameUI();
            }

            cacheUI[i].IsVisible = true;
            cacheUI[i].ToggleHover(false);

            await cacheUI[i].Draw(games[i], ViewGame);

            activeUI.Add(games[i], i);
        }

        hoveredGame = null;
    }

    public abstract void ResetLayout();
    protected abstract GameFilterRequest GetGameFilter();

    protected virtual void CreateGameUI()
    {
        int i = cacheUI.Count;

        Library_Game ui = new Library_Game();
        ui.Width = CardSize.width;
        ui.Height = CardSize.height; // + padding + title
        ui.Margin = new Thickness(0, 0, 2, 0);

        getGameChild.Children.Add(ui);
        cacheUI.Add(ui);

        ui.pointerStatusChange += (enter) => hoveredGame = (enter ? i : null);
    }

    protected virtual void ViewGame(int? id)
    {
        if (id == null)
            return;

        hoveredGame = activeUI[id.Value];
        library.ToggleGameView(id.Value);
    }

    public virtual async Task RefreshGame(int gameId)
    {
        if (activeUI.TryGetValue(gameId, out int uiPos))
            await cacheUI[uiPos].RedrawGameDetails(gameId);
    }

    public virtual void UpdateImage(int gameId, ImageBrush? brush)
    {
        if (activeUI.TryGetValue(gameId, out int uiPos))
            cacheUI[uiPos].RedrawIcon(gameId, brush);
    }

}
