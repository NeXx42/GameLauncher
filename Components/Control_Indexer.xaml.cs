using GameLibary.Components.Indexer;
using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for Control_Indexer.xaml
    /// </summary>
    public partial class Control_Indexer : UserControl
    {
        private Stack<GameFolder> avalibleImports = new Stack<GameFolder>();
        private Action onReimportGames;

        public Control_Indexer()
        {
            InitializeComponent();

            btn_Search.MouseLeftButtonDown += (_, __) => ScanDirectory();
            btn_Import.MouseLeftButtonDown += async (_, __) => await AttemptImport();
        }

        public void Setup(Action onReimport)
        {
            onReimportGames = onReimport;
        }


        private void ScanDirectory()
        {
            List<GameFolder> foundGames = CrawlGames();

            cont_FoundGames.Children.Clear();

            foreach (GameFolder gameFolder in foundGames)
            {
                Control_Indexer_Entry ui = new Control_Indexer_Entry();
                ui.Draw(gameFolder);

                ui.Height = 30;
                ui.Margin = new System.Windows.Thickness(0, 0, 0, 5);

                cont_FoundGames.Children.Add(ui);
                avalibleImports.Push(gameFolder);
            }
        }

        private async Task AttemptImport()
        {
            if(MessageBox.Show("Are you sure you want to import the following games?", "Import", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            while (avalibleImports.TryPop(out GameFolder folder))
            {
                try
                {
                    string exectuable = folder.exectuables.FirstOrDefault() ?? "";

                    dbo_Game newGame = new dbo_Game
                    {
                        gameName = Path.GetFileName(folder.path),
                        executablePath = exectuable
                    };

                    (bool isInvalid, _) = await FileManager.TryMigrate(newGame);

                    if (!isInvalid)
                    {
                        await DatabaseHandler.InsertIntoTable(newGame);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Failed to import game", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show("Done", "Done", MessageBoxButton.OK);

            ScanDirectory();
            await LibaryHandler.RedetectGames();

            onReimportGames?.Invoke();
        }

        private static List<GameFolder> CrawlGames()
        {
            List<GameFolder> foundGameFolders = new List<GameFolder>();
            List<GameZip> foundZips = new List<GameZip>();

            Craw(MainWindow.GameRootLocation);

            foreach (GameZip zip in foundZips)
            {
                Button btn = new Button();
                btn.Content = System.IO.Path.GetFileName(zip.path);

                //cont_zips.Children.Add(btn);
            }


            void Craw(string path)
            {
                if (string.Equals(path, FileManager.GetProcessGameLocation(), StringComparison.CurrentCultureIgnoreCase))
                    return;

                string[] allFiles = Directory.GetFiles(path);
                string[] binaries = allFiles.Where(x => x.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)).ToArray();

                if (binaries.Length > 0)
                {
                    foundGameFolders.Add(new GameFolder()
                    {
                        path = path,
                        exectuables = binaries
                    });
                    return;
                }

                string[] zips = allFiles.Where(IsZip).ToArray();

                if (zips.Length > 0)
                {
                    foreach (string zip in zips)
                    {
                        foundZips.Add(new GameZip()
                        {
                            path = zip
                        });
                    }
                }

                string[] subDirs = Directory.GetDirectories(path);

                foreach (string dir in subDirs)
                {
                    Craw(dir);
                }
            }

            return foundGameFolders;
        }

        private static bool IsZip(string path)
        {
            return path.EndsWith(".7z", StringComparison.InvariantCultureIgnoreCase) ||
                path.EndsWith(".rar", StringComparison.InvariantCultureIgnoreCase);
        }


        public struct GameFolder
        {
            public string path;
            public string[] exectuables;
        }

        private struct GameZip
        {
            public string path;
        }
    }
}
