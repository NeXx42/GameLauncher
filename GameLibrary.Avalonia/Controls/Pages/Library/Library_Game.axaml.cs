using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Avalonia.Controls.Pages.Library;

public partial class Library_Game : UserControl
{
    private Action? onClick;
    private int? gameId;

    public Library_Game()
    {
        InitializeComponent();
        this.PointerPressed += (_, __) => onClick?.Invoke();
    }

    public void DrawSkeleton()
    {
        onClick = null;
        gameId = null;

        img.Background = null;
        title.Text = "";
    }

    public async Task Draw(int gameId, Action<int?> onLaunch)
    {
        if (this.gameId == gameId)
            return;

        this.gameId = gameId;
        await RedrawGameDetails(gameId);

        onClick = () => onLaunch?.Invoke(gameId);
    }

    public async Task RedrawGameDetails(int gameId, bool refetchImage = true)
    {
        GameDto? game = LibraryHandler.TryGetCachedGame(gameId);

        if (game == null)
            return;

        if (refetchImage)
        {
            img.Background = null;
            await ImageManager.GetGameImage<ImageBrush>(game, RedrawIcon);
        }

        title.Text = game.gameName;
    }

    public void RedrawIcon(int gameId, ImageBrush? bitmapImg)
    {
        img.Background = bitmapImg;
    }
}