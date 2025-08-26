using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Control_GameView.xaml
    /// </summary>
    public partial class Control_GameView : UserControl
    {
        private int inspectingGameId;

        private HashSet<int> gameTags;
        private Dictionary<int, Button> allTags = new Dictionary<int, Button>();

        public Control_GameView()
        {
            InitializeComponent();

            btn_Browse.Click += (_, __) => BrowseToGame();
            btn_Launch.Click += (_, __) => HandleLaunch();

            inp_Emulate.Checked += (_, __) => HandleEmulateToggle(true);
            inp_Emulate.Unchecked += (_, __) => HandleEmulateToggle(false);
        }

        public void Setup(MainWindow master)
        {
        }


        public void Draw(dbo_Game game)
        {
            inspectingGameId = game.id;

            img_bg.Source = null;

            if (File.Exists(game.GetRealIconPath))
            {
                img_bg.Source = new BitmapImage(new Uri(game.GetRealIconPath));
            }

            RedrawSelectedTags();

            inp_Emulate.IsChecked = game.useEmulator;
            lbl_Title.Content = game.gameName;
        }

        private void RedrawSelectedTags()
        {
            gameTags = LibaryHandler.GetGameTags(inspectingGameId).ToHashSet();

            foreach (KeyValuePair<int, Button> tag in allTags)
            {
                tag.Value.BorderThickness = gameTags.Contains(tag.Key) ? new Thickness(5) : new Thickness(0);
            }
        }

        private void HandleLaunch()
        {
            GameLauncher.LaunchGame(inspectingGameId);
        }

        private void btn_RegenMedia_Click(object sender, RoutedEventArgs e)
        {
            LibaryHandler.UpdateGameIcon(inspectingGameId, "");
            Draw(LibaryHandler.GetGameFromId(inspectingGameId));
        }

        public void RedrawTags(int[] tags)
        {
            allTags.Clear();
            cont_AllTags.Children.Clear();

            foreach(int tagId in tags)
            {
                GenerateTag(tagId);
            }

            void GenerateTag(int tagId)
            {
                dbo_Tag tag = LibaryHandler.GetTagById(tagId);

                Button btn = new Button();
                btn.Content = tag.TagName;
                btn.Width = 150;
                btn.Height = 30;

                btn.Click += (_, __) => HandleTagToggle(tagId);

                cont_AllTags.Children.Add(btn);
                allTags.Add(tagId, btn);
            }
        }

        private void HandleTagToggle(int tagId)
        {
            if (gameTags.Contains(tagId))
            {
                gameTags.Remove(tagId);
                LibaryHandler.RemoveTagFromGame(inspectingGameId, tagId);
            }
            else
            {
                gameTags.Add(tagId);
                LibaryHandler.AddTagToGame(inspectingGameId, tagId);
            }

            RedrawSelectedTags();
        }

        private void HandleEmulateToggle(bool to)
        {
            LibaryHandler.UpdateGameEmulationStatus(inspectingGameId, to);           
        }

        private void btn_Overlay_Click(object sender, RoutedEventArgs e)
        {
            GameLauncher.RequestOverlay(inspectingGameId, null);
        }

        private void BrowseToGame()
        {
            Process.Start("explorer.exe", Path.GetDirectoryName(LibaryHandler.GetGameFromId(inspectingGameId).executablePath));
        }
    }
}
