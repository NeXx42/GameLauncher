using GameLibary.Components;
using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GameLibary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string CONFIG_ROOTLOCATION = "RootPath";
        public const string CONFIG_EMULATORLOCATION = "EmulatorPath";
        public const string CONFIG_PASSWORD = "PasswordHash";

        public static string? GameRootLocation;
        public static string? EmulatorLocation;

        public static MainWindow? window;

        private bool inSetup = true;
        private int gamesSlide;

        private HashSet<int> activeTags = new HashSet<int>();
        private Dictionary<int, Element_Tag> existingTags = new Dictionary<int, Element_Tag>();


        public MainWindow()
        {
            inSetup = true;
            window = this;

            InitializeComponent();

            GameViewer.MouseLeftButtonDown += (_, e) => e.Handled = true;
            SetupHandler.MouseLeftButtonDown += (_, e) => e.Handled = true;
            TagCreator.MouseLeftButtonDown += (_, e) => e.Handled = true;

            FileManager.Setup();
            DatabaseHandler.Setup();
            GameLauncher.Setup();

            CheckForSetup();

            WindowState = WindowState.Maximized;
        }


        public void CheckForSetup()
        {
            ToggleMenu(true, true);

            if(!IsValidSave(out GameRootLocation, out EmulatorLocation, out bool requireLogin))
            {
                SetupHandler.Visibility = Visibility.Visible;
                return;
            }

            if (requireLogin)
            {
                LoginHandler.Visibility = Visibility.Visible;
            }
            else
            {
                CompleteLoad();
            }
        }

        private bool IsValidSave(out string dir, out string emu, out bool useLogin)
        {
            dbo_Config? mainDirectory = DatabaseHandler.GetItems<dbo_Config>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Config.key), CONFIG_ROOTLOCATION)).FirstOrDefault();
            dbo_Config? emulatorDirectory = DatabaseHandler.GetItems<dbo_Config>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Config.key), CONFIG_EMULATORLOCATION)).FirstOrDefault();

            if (mainDirectory != null && emulatorDirectory != null)
            {
                dir = mainDirectory!.value;
                emu = emulatorDirectory!.value;

                useLogin = DatabaseHandler.GetItems<dbo_Config>(new DatabaseHandler.QueryBuilder().SearchEquals(nameof(dbo_Config.key), CONFIG_PASSWORD)).Length > 0;

                return true;
            }

            dir = "";
            emu = "";
            useLogin = false;

            return false;
        }

        public async void CompleteLoad()
        {
            await LibaryHandler.Setup();

            GameViewer.Setup(this);

            inSetup = false;
            ToggleMenu(false);

            DrawGames();
            DrawTags();
        }



        public void DrawGames()
        {
            const float ratio = 0.1927083333f;
            const int columnLimit = 5;

            lbl_GamePos.Content = $"Games - {gamesSlide}";
            cont_Games.Children.Clear();

            int[] games = LibaryHandler.GetDrawList(gamesSlide, columnLimit * 3);

            foreach(int gameId in games)
                DrawGame(gameId);

            void DrawGame(int gameId)
            {
                Control_Game ui = new Control_Game();
                ui.Width = (1920 * ratio) + 5;
                ui.Height = (1080 * ratio) + 5 + 50; // + padding + title
                ui.Margin = new Thickness(0, 0, 2, 0);

                ui.Draw(gameId, ToggleGameView);

                cont_Games.Children.Add(ui);
            }
        }

        public void DrawTags()
        {
            existingTags.Clear();
            activeTags.Clear();

            cont_AllTags.Children.Clear();
            cont_TagSearch.Children.Clear();

            int[] tagIds = LibaryHandler.GetAllTags();

            foreach (int tagId in tagIds) 
            {
                if (existingTags.ContainsKey(tagId))
                    continue;

                existingTags.Add(tagId, CreateTagUI(tagId));
            }

            GameViewer.RedrawTags(tagIds);

            Element_Tag CreateTagUI(int tagId)
            {
                dbo_Tag? tag = LibaryHandler.GetTagById(tagId);

                Element_Tag ui = new Element_Tag();
                ui.Draw(tag, SwapTagMode);

                cont_AllTags.Children.Add(ui);
                return ui;
            }
        }

        private void SwapTagMode(int tagId)
        {
            if (existingTags.TryGetValue(tagId, out Element_Tag ui))
            {
                if (activeTags.Contains(tagId))
                {
                    activeTags.Remove(tagId);
                    ui.Toggle(false);
                }
                else
                {
                    activeTags.Add(tagId);
                    ui.Toggle(true);
                }
            }

            LibaryHandler.RefilterGames(activeTags);
            gamesSlide = 0;

            DrawGames();
        }


        private void ToggleGameView(int gameId)
        {
            dbo_Game? game = LibaryHandler.GetGameFromId(gameId);

            if (game != null)
            {
                GameViewer.Draw(game);
            }

            ToggleMenu(true);
            GameViewer.Visibility = Visibility.Visible;
        }

        public void OpenTagCreator()
        {
            ToggleMenu(true);
            TagCreator.Visibility = Visibility.Visible;
        }


        public void ToggleMenu(bool to, bool force = false)
        {
            if (inSetup || force)
                return;

            effect_blur.Radius = to ? 10 : 0;
            cont_MenuView.Visibility = to ? Visibility.Visible : Visibility.Hidden;

            TagCreator.Visibility = Visibility.Hidden;
            GameViewer.Visibility = Visibility.Hidden;
            SetupHandler.Visibility = Visibility.Hidden;
            LoginHandler.Visibility = Visibility.Hidden;
        }

        private void btn_CreateTag_Click(object sender, RoutedEventArgs e)
        {
            OpenTagCreator();
        }

        private void cont_MenuView_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToggleMenu(false);
        }


        private void btn_PrevPage_Click(object sender, RoutedEventArgs e)
        {
            gamesSlide -= (5 * 3);
            gamesSlide = Math.Max(0, gamesSlide);

            DrawGames();
        }

        private void btn_NextPage_Click(object sender, RoutedEventArgs e)
        {
            float interval = 5f * 3f;
            int maxSlide = ((int)Math.Floor(LibaryHandler.GetFilteredGameCount() / interval)) * (int)interval;

            gamesSlide += (int)interval;
            gamesSlide = Math.Min(gamesSlide, maxSlide);

            DrawGames();
        }
    }
}