using System.Windows;
using System.Windows.Controls;
using GameLibary.Components;
using GameLibary.Source;
using GameLibary.Source.Database.Tables;

namespace GameLibary.Pages
{
    /// <summary>
    /// Interaction logic for Page_Content.xaml
    /// </summary>
    public partial class Page_Content : Page
    {
        public static (int x, int y) ContentPerPage = (5, 4);
        public int getTotalContentPerPage => ContentPerPage.x * ContentPerPage.y;

        private int gamesSlide;

        private HashSet<int> activeTags = new HashSet<int>();
        private Dictionary<int, Element_Tag> existingTags = new Dictionary<int, Element_Tag>();

        private bool currentSortAscending = true;
        private LibaryHandler.OrderType currentSort = LibaryHandler.OrderType.Id;


        public Page_Content()
        {
            _ = LibaryHandler.Setup();
            LibaryHandler.onGlobalImageSet += async (i, b) => await DrawGames();

            InitializeComponent();

            BindButtons();
            ToggleMenu(false);

            DrawEverything();
        }

        private async void DrawEverything()
        {
            ToggleTagCreator(false);

            await DrawGames();
            await DrawTags();

            RedrawSortNames();
        }

        private void BindButtons()
        {
            Indexer.Setup(() => RefilterGames());
            GameViewer.Setup(this);

            GameViewer.MouseLeftButtonDown += (_, e) => e.Handled = true;
            Indexer.MouseLeftButtonDown += (_, e) => e.Handled = true;
            Settings.MouseLeftButtonDown += (_, e) => e.Handled = true;

            btn_OpenTagCreator.RegisterClick(() => ToggleTagCreator(null));
            btn_CreateTag.MouseLeftButtonDown += (_, __) => CreateTag();

            btn_FirstPage.MouseLeftButtonDown += (_, __) => FirstPage();
            btn_PrevPage.MouseLeftButtonDown += (_, __) => PrePage();
            btn_NextPage.MouseLeftButtonDown += (_, __) => NextPage();
            btn_LastPage.MouseLeftButtonDown += (_, __) => LastPage();

            btn_Indexer.MouseLeftButtonDown += (_, __) => OpenIndexer();
            btn_Settings.MouseLeftButtonDown += (_, __) => OpenSettings();

            btn_SortDir.RegisterClick(UpdateSortDirection);
            inp_SortType.Setup(Enum.GetValues(typeof(LibaryHandler.OrderType)), 0, UpdateSortType);
        }


        public async Task DrawGames()
        {
            const float ratio = 0.1927083333f;

            lbl_PageNum.Text = $"{(gamesSlide / getTotalContentPerPage) + 1}";
            cont_Games.Children.Clear();

            int[] games = await LibaryHandler.GetDrawList(gamesSlide, getTotalContentPerPage);

            foreach (int gameId in games)
                DrawGame(gameId);

            void DrawGame(int gameId)
            {
                Control_Game ui = new Control_Game();
                ui.Width = (1920 * ratio);
                ui.Height = (1080 * ratio); // + padding + title
                ui.Margin = new Thickness(0, 0, 2, 0);

                ui.Draw(gameId, ToggleGameView);

                cont_Games.Children.Add(ui);
            }
        }

        public async Task DrawTags()
        {
            existingTags.Clear();
            activeTags.Clear();

            cont_AllTags.Children.Clear();

            int[] tagIds = await LibaryHandler.GetAllTags();

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
                ui.VerticalContentAlignment = VerticalAlignment.Stretch;
                ui.Draw(tag, SwapTagMode);

                cont_AllTags.Children.Add(ui);
                return ui;
            }
        }

        private async void SwapTagMode(int tagId)
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

            await RefilterGames();
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

        private async Task RefilterGames(bool resetPage = true)
        {
            if (resetPage)
                gamesSlide = 0;

            await LibaryHandler.RefilterGames(activeTags, currentSort, currentSortAscending);
            await DrawGames();
        }


        private async void ToggleGameView(int gameId)
        {
            dbo_Game? game = LibaryHandler.GetGameFromId(gameId);

            if (game != null)
            {
                await GameViewer.Draw(game);
            }

            ToggleMenu(true);
            GameViewer.Visibility = Visibility.Visible;
        }


        public void ToggleMenu(bool to)
        {
            effect_blur.Radius = to ? 10 : 0;
            cont_MenuView.Visibility = to ? Visibility.Visible : Visibility.Hidden;

            GameViewer.Visibility = Visibility.Hidden;
            Settings.Visibility = Visibility.Hidden;
            Indexer.Visibility = Visibility.Hidden;
        }


        private void cont_MenuView_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToggleMenu(false);
        }



        private void OpenIndexer()
        {
            ToggleMenu(true);
            Indexer.Visibility = Visibility.Visible;
            Indexer.OnOpen();
        }
        private void OpenSettings()
        {
            ToggleMenu(true);
            Settings.Visibility = Visibility.Visible;
            Settings.OnOpen();
        }

        private void ToggleTagCreator(bool? to)
        {
            if (!to.HasValue)
            {
                to = !(cont_TagCreator.Visibility == Visibility.Visible);
            }

            cont_TagCreator.Visibility = to.Value ? Visibility.Visible : Visibility.Hidden;
            btn_OpenTagCreator.Label = to.Value ? $"Close" : "+ Add Tag";
        }


        // sort
        private async void UpdateSortDirection()
        {
            currentSortAscending = !currentSortAscending;
            RedrawSortNames();

            await RefilterGames();
        }

        private async void UpdateSortType()
        {
            currentSort = (LibaryHandler.OrderType)inp_SortType.selectedIndex;
            await RefilterGames();
        }

        private void RedrawSortNames()
        {
            btn_SortDir.Label = currentSortAscending ? "Ascending" : "Descending";
        }


        // page controls


        private void PrePage()
        {
            gamesSlide -= getTotalContentPerPage;
            gamesSlide = Math.Max(0, gamesSlide);

            DrawGames();
        }

        private void NextPage()
        {
            int maxSlide = ((int)Math.Floor(LibaryHandler.GetFilteredGameCount() / (float)getTotalContentPerPage)) * getTotalContentPerPage;

            gamesSlide += (int)getTotalContentPerPage;
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
            gamesSlide = ((int)Math.Floor(LibaryHandler.GetFilteredGameCount() / (float)getTotalContentPerPage)) * getTotalContentPerPage;
            DrawGames();
        }
    }
}
