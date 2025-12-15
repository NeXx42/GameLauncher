using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Controls;

public partial class Library_Game : UserControl
{
    private Action? onClick;
    private int gameId;

    public Library_Game()
    {
        InitializeComponent();
        this.PointerPressed += (_, __) => onClick?.Invoke();
    }

    public async Task Draw(int gameId, Action<int> onLaunch)
    {
        if (this.gameId == gameId)
            return;

        img.Background = null;

        this.gameId = gameId;
        dbo_Game? game = LibraryHandler.GetGameFromId(gameId);

        if (game != null)
        {
            title.Text = game.gameName;
            await ImageManager.GetGameImage<ImageBrush>(game, RedrawIcon);
        }

        onClick = () => onLaunch?.Invoke(gameId);
    }

    public void RedrawIcon(int gameId, ImageBrush? bitmapImg)
    {
        if (this.gameId != gameId)
            return;

        img.Background = bitmapImg;
    }
}