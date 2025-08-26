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
        private Dictionary<int, Element_Tag> allTags = new Dictionary<int, Element_Tag>();

        private bool ignoredComboboxEvents;

        public Control_GameView()
        {
            InitializeComponent();

            btn_Browse.Click += (_, __) => BrowseToGame();
            btn_Launch.Click += (_, __) => HandleLaunch();

            inp_Emulate.Checked += (_, __) => HandleEmulateToggle(true);
            inp_Emulate.Unchecked += (_, __) => HandleEmulateToggle(false);

            inp_binary.SelectionChanged += (_, __) => HandleBinaryChange();
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

            List<string> executableBinaries = GetBinaries(game.gameName);

            ignoredComboboxEvents = true;
            inp_binary.ItemsSource = executableBinaries.Select(x => Path.GetFileName(x));
            inp_binary.SelectedIndex = executableBinaries.IndexOf(game.executablePath.Substring(1));
            ignoredComboboxEvents = false;
        }

        private List<string> GetBinaries(string gameName)
        {
            string gameFolder = Path.Combine(FileManager.GetProcessGameLocation(), gameName);
            return Directory.GetFiles(gameFolder).Where(x => x.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)).Select(x => Path.GetFileName(x)).ToList();
        }

        private void RedrawSelectedTags()
        {
            gameTags = LibaryHandler.GetGameTags(inspectingGameId).ToHashSet();

            foreach (KeyValuePair<int, Element_Tag> tag in allTags)
            {
                tag.Value.Toggle(gameTags.Contains(tag.Key));
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

                Element_Tag tagUI = new Element_Tag();
                tagUI.Draw(tag, HandleTagToggle);

                cont_AllTags.Children.Add(tagUI);
                allTags.Add(tagId, tagUI);
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

        private void HandleBinaryChange()
        {
            if (ignoredComboboxEvents)
                return;

            LibaryHandler.ChangeBinaryLocation(inspectingGameId, inp_binary.SelectedValue?.ToString());
            Draw(LibaryHandler.GetGameFromId(inspectingGameId));
        }
    }
}
