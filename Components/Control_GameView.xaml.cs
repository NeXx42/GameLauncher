using GameLibary.Pages;
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
        private Page_Content master;

        public Control_GameView()
        {
            InitializeComponent();

            btn_Delete.RegisterClick(DeleteGame);
            btn_RegenMedia.RegisterClick(btn_RegenMedia_Click);
            btn_Overlay.RegisterClick(btn_Overlay_Click);

            btn_Browse.RegisterClick(BrowseToGame);
            btn_Launch.RegisterClick(HandleLaunch);

            inp_Emulate.Checked += (_, __) => HandleEmulateToggle(true);
            inp_Emulate.Unchecked += (_, __) => HandleEmulateToggle(false);

            inp_binary.SelectionChanged += (_, __) => HandleBinaryChange();
        }

        public void Setup(Page_Content master)
        {
            this.master = master;
            LibaryHandler.onGlobalImageSet += (i, p) => UpdateGameIcon(i, p);
        }


        public void Draw(dbo_Game game)
        {
            inspectingGameId = game.id;

            img_bg.Source = null;
            LibaryHandler.GetGameImage(game, UpdateGameIcon);

            RedrawSelectedTags();

            inp_Emulate.IsChecked = game.useEmulator;
            lbl_Title.Content = game.gameName;

            List<string> executableBinaries = GetBinaries(game.gameName);

            ignoredComboboxEvents = true;
            inp_binary.ItemsSource = executableBinaries.Select(x => Path.GetFileName(x));
            inp_binary.SelectedIndex = executableBinaries.IndexOf(game.executablePath.Substring(1));
            ignoredComboboxEvents = false;
        }

        private void UpdateGameIcon(int gameId, BitmapImage? img)
        {
            if (inspectingGameId != gameId)
                return;

            img_bg.Source = img;
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

        private void btn_Overlay_Click()
        {
            GameLauncher.RequestOverlay(inspectingGameId, null);
        }
        private void btn_RegenMedia_Click()
        {
            UpdateGameIcon(inspectingGameId, null);

            LibaryHandler.UpdateGameIcon(inspectingGameId, "");
            Draw(LibaryHandler.GetGameFromId(inspectingGameId));
        }

        private void BrowseToGame() => FileManager.BrowseToGame(LibaryHandler.GetGameFromId(inspectingGameId)!);
        private void HandleBinaryChange()
        {
            if (ignoredComboboxEvents)
                return;

            LibaryHandler.ChangeBinaryLocation(inspectingGameId, inp_binary.SelectedValue?.ToString());
            Draw(LibaryHandler.GetGameFromId(inspectingGameId)!);
        }

        private async void DeleteGame()
        {
            dbo_Game? game = LibaryHandler.GetGameFromId(inspectingGameId);

            if (game != null && MessageBox.Show($"Are you sure you want to delete '{game.gameName}'", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RetryLogic();
            }

            async void RetryLogic()
            {
                Exception? error = await LibaryHandler.DeleteGame(game!);

                if (error == null)
                {
                    master.DrawGames();
                    master.ToggleMenu(false);
                }
                else if(MessageBox.Show(error.Message, "Retry", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    RetryLogic();
                }
            }
        }
    }
}
