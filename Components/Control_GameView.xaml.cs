using GameLibary.Pages;
using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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

        private Page_Content master;

        public Control_GameView()
        {
            InitializeComponent();

            btn_Delete.RegisterClick(DeleteGame);
            btn_Overlay.RegisterClick(btn_Overlay_Click);

            btn_Browse.RegisterClick(BrowseToGame);
            btn_Launch.RegisterClick(HandleLaunch);

            inp_Emulate.RegisterOnChange(HandleEmulateToggle);
        }

        public void Setup(Page_Content master)
        {
            this.master = master;
            LibaryHandler.onGlobalImageSet += (i, p) => UpdateGameIcon(i, p);
        }


        public async Task Draw(dbo_Game game)
        {
            inspectingGameId = game.id;

            img_bg.ImageSource = null;
            LibaryHandler.GetGameImage(game, UpdateGameIcon);

            await RedrawSelectedTags();

            inp_Emulate.ToggleSilent(game.useEmulator);
            lbl_Title.Content = game.gameName;

            List<string> executableBinaries = await GetBinaries(game);
            inp_binary.Setup(executableBinaries.Select(x => Path.GetFileName(x)), executableBinaries.IndexOf(game.executablePath!), HandleBinaryChange);
        }

        private void UpdateGameIcon(int gameId, BitmapImage? img)
        {
            if (inspectingGameId != gameId)
                return;

            img_bg.ImageSource = img;
        }

        private async Task<List<string>> GetBinaries(dbo_Game game)
        {
            string gameFolder = await game.GetFolderLocation();
            return Directory.GetFiles(gameFolder).Where(x => x.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)).Select(x => Path.GetFileName(x)).ToList();
        }

        private async Task RedrawSelectedTags()
        {
            gameTags = (await LibaryHandler.GetGameTags(inspectingGameId)).ToHashSet();

            foreach (KeyValuePair<int, Element_Tag> tag in allTags)
            {
                tag.Value.Margin = new Thickness(0, 0, 0, 5);
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

            foreach (int tagId in tags)
            {
                GenerateTag(tagId);
            }

            void GenerateTag(int tagId)
            {
                dbo_Tag? tag = LibaryHandler.GetTagById(tagId);

                if (tag != null)
                {
                    Element_Tag tagUI = new Element_Tag();
                    tagUI.Draw(tag, HandleTagToggle);

                    cont_AllTags.Children.Add(tagUI);
                    allTags.Add(tagId, tagUI);
                }
            }
        }

        private async void HandleTagToggle(int tagId)
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

            await RedrawSelectedTags();
        }

        private void HandleEmulateToggle(bool to)
        {
            LibaryHandler.UpdateGameEmulationStatus(inspectingGameId, to);
        }

        private void btn_Overlay_Click()
        {
            GameLauncher.RequestOverlay(inspectingGameId, null);
        }

        private async void BrowseToGame() => await FileManager.BrowseToGame(LibaryHandler.GetGameFromId(inspectingGameId)!);

        private async void HandleBinaryChange()
        {
            await LibaryHandler.ChangeBinaryLocation(inspectingGameId, inp_binary.selectedValue?.ToString());
            await Draw(LibaryHandler.GetGameFromId(inspectingGameId)!);
        }

        private async void DeleteGame()
        {
            dbo_Game? game = LibaryHandler.GetGameFromId(inspectingGameId);

            if (game != null && MessageBox.Show($"Are you sure you want to delete '{game.gameName}'\n'{await game.GetFolderLocation()}'", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RetryLogic();
            }

            async void RetryLogic()
            {
                Exception? error = await LibaryHandler.DeleteGame(game!);

                if (error == null)
                {
                    await master.DrawGames();
                    master.ToggleMenu(false);
                }
                else
                {
                    MessageBox.Show(error.Message, "Failed to delete record", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
