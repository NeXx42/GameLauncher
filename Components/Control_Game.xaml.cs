using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Control_Game.xaml
    /// </summary>
    public partial class Control_Game : UserControl
    {
        private Action onClick;
        private int gameId;

        public Control_Game()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (_, __) => onClick?.Invoke();
        }

        public void Draw(int gameId, Action<int> onLaunch)
        {
            if (this.gameId == gameId)
                return;

            img.ImageSource = null;

            this.gameId = gameId;
            dbo_Game? game = LibaryHandler.GetGameFromId(gameId);

            if (game != null)
            {
                title.Text = game.gameName;
                LibaryHandler.GetGameImage(game, RedrawIcon);
            }

            onClick = () => onLaunch?.Invoke(gameId);
        }

        private void RedrawIcon(int gameId, BitmapImage bitmapImg)
        {
            if (this.gameId != gameId)
                return;

            img.ImageSource = bitmapImg;
        }
    }
}
