using GameLibary.Pages;
using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using System.Windows;
using System.Windows.Controls;

namespace GameLibary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool requiresPassword;
        public static string? EmulatorLocation;

        public static MainWindow? window;

        private Dictionary<string, Page> generatedPages = new Dictionary<string, Page>();

        public MainWindow()
        {
            window = this;

            InitializeComponent();

            UpdateActiveBanner(null);
            btn_Banner_Detach.RegisterClick(GameLauncher.DetachPlayingGame);

            FileManager.Setup();
            DatabaseHandler.Setup();

            CheckForSetup();

            WindowState = WindowState.Maximized;
        }


        public async void CheckForSetup()
        {
            if (!(await IsSaveValid()))
            {
                LoadPage<Page_Setup>();
                return;
            }

            if (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.PasswordHash) != null)
            {
                LoadPage<Page_Lock>();
            }
            else
            {
                LoadPage<Page_Content>();
            }
        }

        public async Task<bool> IsSaveValid()
        {
            bool libaryExists = await DatabaseHandler.Exists<dbo_Libraries>();
            EmulatorLocation = (await ConfigHandler.GetConfigValue(ConfigHandler.ConfigValues.EmulatorPath))?.value;

            return libaryExists && EmulatorLocation != null;
        }

        public void LoadPage<T>() where T : Page
        {
            string pageName = typeof(T).FullName!;

            if (!generatedPages.ContainsKey(pageName))
            {
                generatedPages.Add(pageName, (Page)Activator.CreateInstance(typeof(T))!);
            }

            MainPage.Navigate(generatedPages[pageName]);
        }


        public void UpdateActiveBanner(string? playing = null)
        {
            if (string.IsNullOrEmpty(playing))
            {
                Banner_ActiveGame.Visibility = Visibility.Hidden;
            }
            else
            {
                Banner_ActiveGame.Visibility = Visibility.Visible;
                Banner_Text.Content = playing;
            }
        }
    }
}