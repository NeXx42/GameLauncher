using GameLibary.Components;
using GameLibary.Source.Database.Tables;
using GameLibary.Source;
using System;
using System.Collections.Generic;
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

namespace GameLibary.Pages
{
    /// <summary>
    /// Interaction logic for Page_Content.xaml
    /// </summary>
    public partial class Page_Content : Page
    {
        private static int getGamesPerPage => 5 * 3;

        private int gamesSlide;

        private HashSet<int> activeTags = new HashSet<int>();
        private Dictionary<int, Element_Tag> existingTags = new Dictionary<int, Element_Tag>();


        public Page_Content()
        {
            _ = LibaryHandler.Setup();
            LibaryHandler.onGlobalImageSet += (i, b) => DrawGames();

            InitializeComponent();

            BindButtons();

            ToggleMenu(false);

            DrawGames();
            DrawTags();
        }

        private void BindButtons()
        {
            Indexer.Setup(DrawGames);
            GameViewer.Setup(this);

            GameViewer.MouseLeftButtonDown += (_, e) => e.Handled = true;
            Indexer.MouseLeftButtonDown += (_, e) => e.Handled = true;

            btn_CreateTag.MouseLeftButtonDown += (_, __) => CreateTag();

            btn_FirstPage.MouseLeftButtonDown += (_, __) => FirstPage();
            btn_PrevPage.MouseLeftButtonDown += (_, __) => PrePage();
            btn_NextPage.MouseLeftButtonDown += (_, __) => NextPage();
            btn_LastPage.MouseLeftButtonDown += (_, __) => LastPage();

            btn_Indexer.MouseLeftButtonDown += (_, __) => OpenIndexer();

            combo_OrderType.ItemsSource = System.Enum.GetValues(typeof(LibaryHandler.OrderType));
            combo_OrderType.SelectedIndex = 0;

            combo_OrderType.SelectionChanged += (_, __) => RefilterGames();
        }

        public void DrawGames()
        {
            const float ratio = 0.1927083333f;
            const int columnLimit = 5;

            lbl_PageNum.Text = $"{(gamesSlide / getGamesPerPage) + 1}";
            cont_Games.Children.Clear();

            int[] games = LibaryHandler.GetDrawList(gamesSlide, columnLimit * 3);

            foreach (int gameId in games)
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

            RefilterGames();
        }

        private void RefilterGames()
        {
            LibaryHandler.RefilterGames(activeTags, (LibaryHandler.OrderType)combo_OrderType.SelectedIndex);
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


        public void ToggleMenu(bool to)
        {
            effect_blur.Radius = to ? 10 : 0;
            cont_MenuView.Visibility = to ? Visibility.Visible : Visibility.Hidden;

            GameViewer.Visibility = Visibility.Hidden;
            Indexer.Visibility = Visibility.Hidden;
        }

        private async void CreateTag()
        {
            string newTagName = inp_TagName.Text;

            if (string.IsNullOrEmpty(newTagName))
                return;

            inp_TagName.Text = "";

            await DatabaseHandler.InsertIntoTable(new dbo_Tag()
            {
                TagName = newTagName,
            });

            LibaryHandler.MarkTagsAsDirty();

            DrawTags();
        }

        private void cont_MenuView_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToggleMenu(false);
        }



        private void OpenIndexer()
        {
            ToggleMenu(true);
            Indexer.Visibility = Visibility.Visible;
        }



        // page controls


        private void PrePage()
        {
            gamesSlide -= getGamesPerPage;
            gamesSlide = Math.Max(0, gamesSlide);

            DrawGames();
        }

        private void NextPage()
        {
            int maxSlide = ((int)Math.Floor(LibaryHandler.GetFilteredGameCount() / (float)getGamesPerPage)) * getGamesPerPage;

            gamesSlide += (int)getGamesPerPage;
            gamesSlide = Math.Min(gamesSlide, maxSlide);

            DrawGames();
        }

        private void FirstPage()
        {
            gamesSlide = 0;
            DrawGames();
        }

        private void LastPage()
        {
            gamesSlide = ((int)Math.Floor(LibaryHandler.GetFilteredGameCount() / (float)getGamesPerPage)) * getGamesPerPage;
            DrawGames();
        }
    }
}
