using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.AvaloniaUI.Controls.Pages.Library;

public partial class Library_Game : UserControl
{
    private static IBrush? noBGBrush
    {
        get
        {
            m_noBGBrush ??= new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            return m_noBGBrush;
        }
    }
    private static IBrush? m_noBGBrush;

    public Action<bool>? pointerStatusChange;

    private Action? onClick;
    private int? gameId;

    public Library_Game()
    {
        InitializeComponent();

        this.PointerPressed += (_, __) => onClick?.Invoke();

        this.PointerEntered += (_, __) => pointerStatusChange?.Invoke(true);
        this.PointerExited += (_, __) => pointerStatusChange?.Invoke(false);
    }

    public void DrawSkeleton()
    {
        onClick = null;
        gameId = null;

        img.Background = noBGBrush;
        title.Text = "";

        lbl_NoIcon.IsVisible = true;
        lbl_NoIcon.Text = "Loading";
        lbl_LastPlayed.Content = "";

        ToggleHover(false);
    }

    public async Task Draw(int gameId, Action<int?> onLaunch)
    {
        if (this.gameId == gameId)
            return;

        this.gameId = gameId;
        await RedrawGameDetails(gameId);

        onClick = () => onLaunch?.Invoke(gameId);
    }

    public async Task RedrawGameDetails(int gameId)
    {
        img.Background = noBGBrush;
        lbl_NoIcon.Text = "Loading";
        lbl_NoIcon.IsVisible = true;
        lbl_LastPlayed.Content = "";

        GameDto? game = LibraryHandler.TryGetCachedGame(gameId);

        if (game == null)
            return;

        title.Text = game.gameName;
        lbl_NoIcon.Text = game.gameName;
        lbl_LastPlayed.Content = game.GetLastPlayedFormatted();

        DrawTags(game);
        await ImageManager.GetGameImage<ImageBrush>(game, RedrawIcon);
    }

    public void RedrawIcon(int gameId, ImageBrush? bitmapImg)
    {
        img.Background = bitmapImg ?? noBGBrush;
        lbl_NoIcon.IsVisible = bitmapImg == null;
    }

    private void DrawTags(GameDto game)
    {
        TagDto[] tags = TagManager.GetTagsForAGame(game);

        for (int i = 0; i < 3; i++)
        {
            if (i < tags.Count())
            {
                GetTag(i)?.Draw(tags.ElementAt(i));
            }
            else
            {
                GetTag(i)?.Draw(null);
            }
        }

        Library_TagUnselectable? GetTag(int pos)
        {
            switch (pos)
            {
                case 0: return tag_1;
                case 1: return tag_2;
                case 2: return tag_3;
            }

            return null;
        }
    }

    public void ToggleHover(bool to)
    {
        if (to)
        {
            ctrl.Classes.Add("hovered");
        }
        else
        {
            ctrl.Classes.Remove("hovered");
        }
    }
}