using GameLibary.Components.Indexer;
using GameLibary.Source;
using GameLibary.Source.Database.Tables;
using Microsoft.WindowsAPICodePack.Dialogs;
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
        private Func<Task> onReimportGames;

        private dbo_Libraries[] possibleLibaries;

        public Control_Indexer()
        {
            InitializeComponent();

            cont_FoundGames.Children.Clear();

            btn_Search.MouseLeftButtonDown += async (_, __) => await ScanDirectory();
            btn_Import.MouseLeftButtonDown += async (_, __) => await AttemptImport();
        }

        public void Setup(Func<Task> onReimport)
        {
            onReimportGames = onReimport;
        }

        public async void OnOpen()
        {
            possibleLibaries = await DatabaseHandler.GetItems<dbo_Libraries>();

            this.Visibility = Visibility.Visible;
        }


        private async Task ScanDirectory()
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Search Folder"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok && Directory.Exists(dlg.FileName))
            {
                List<GameFolder> foundGames = await CrawlGames(dlg.FileName!);

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
        }

        private async Task AttemptImport()
        {
            dbo_Libraries chosenLibary = possibleLibaries.First();

            if (MessageBox.Show($"Are you sure you want to import the following games into the libary \n'{chosenLibary.rootPath}'?", "Import", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            while (avalibleImports.TryPop(out GameFolder folder))
            {
                try
                {
                    dbo_Game newGame = new dbo_Game
                    {
                        gameName = CorrectGameName(Path.GetFileName(folder.path)),
                        executablePath = TryFindBestExecutable(folder.exectuables),
                        libaryId = chosenLibary.libaryId
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

            await ScanDirectory();
            await LibaryHandler.RedetectGames();

            await onReimportGames();

            string TryFindBestExecutable(string[] possible)
            {
                string? bestPossible = possible.Where(x =>
                {
                    string name = Path.GetFileName(x).ToLower();
                    return !name.Contains("crash", StringComparison.InvariantCulture)
                            && !name.Contains("crash", StringComparison.InvariantCulture);
                }).FirstOrDefault();

                return bestPossible ?? possible.FirstOrDefault() ?? "";
            }

            string CorrectGameName(string existing)
            {
                return existing.Replace("'", "");
            }
        }

        private static async Task<List<GameFolder>> CrawlGames(string path)
        {
            List<GameFolder> foundGameFolders = new List<GameFolder>();
            List<GameZip> foundZips = new List<GameZip>();

            Craw(path);

            foreach (GameZip zip in foundZips)
            {
                Button btn = new Button();
                btn.Content = System.IO.Path.GetFileName(zip.path);

                //cont_zips.Children.Add(btn);
            }


            void Craw(string path)
            {
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
