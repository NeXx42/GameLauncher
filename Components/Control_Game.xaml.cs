using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            this.gameId = gameId;
            dbo_Game? game = LibaryHandler.GetGameFromId(gameId);

            if(game != null)
            {
                title.Content = game.gameName;
                LibaryHandler.GetGameImage(game, RedrawIcon);
            }

            onClick = () => onLaunch?.Invoke(gameId);
        }

        private void RedrawIcon(int gameId, BitmapImage bitmapImg)
        {
            if (this.gameId != gameId)
                return;

            img.Source = bitmapImg;
        }
    }
}
