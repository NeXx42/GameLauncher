using GameLibary.Components;
using GameLibary.Pages;
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

        private Dictionary<string, Page> generatedPages = new Dictionary<string, Page>();

        public MainWindow()
        {
            window = this;

            InitializeComponent();

            UpdateActiveBanner(null);
            btn_Banner_Detach.Click += (_, __) => GameLauncher.DetachPlayingGame();

            FileManager.Setup();
            DatabaseHandler.Setup();

            CheckForSetup();

            WindowState = WindowState.Maximized;
        }


        public void CheckForSetup()
        {
            if(!IsValidSave(out GameRootLocation, out EmulatorLocation, out bool requireLogin))
            {
                LoadPage<Page_Setup>();
                return;
            }

            if (requireLogin)
            {
                LoadPage<Page_Lock>();
            }
            else
            {
                LoadPage<Page_Content>();
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

        public void LoadPage<T>() where T : Page
        {
            string pageName = typeof(T).FullName;

            if(!generatedPages.ContainsKey(pageName))
            {
                generatedPages.Add(pageName, (Page)Activator.CreateInstance(typeof(T)));
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