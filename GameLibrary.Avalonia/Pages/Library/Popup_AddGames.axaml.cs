using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Pages.Library;

public partial class Popup_AddGames : UserControl
{
    private Stack<FileManager.GameFolder> availableImports = new Stack<FileManager.GameFolder>();
    private Func<Task>? onReimportGames;

    private dbo_Libraries[]? possibleLibraries;

    public Popup_AddGames()
    {
        InitializeComponent();

        cont_FoundGames.Children.Clear();

        btn_Search.RegisterClick(ScanDirectory);
        btn_Import.RegisterClick(AttemptImport);
    }


    public void Setup(Func<Task> onReimport)
    {
        onReimportGames = onReimport;
    }

    public async void OnOpen()
    {
        possibleLibraries = await DatabaseHandler.GetItems<dbo_Libraries>();

        this.IsVisible = true;
    }


    private async Task ScanDirectory()
    {
        IReadOnlyList<IStorageFolder> selectedFolders = await DialogHelper.OpenFolderAsync(new FolderPickerOpenOptions()
        {
            Title = "Select Directories",
            AllowMultiple = true,
        });

        if (selectedFolders.Count > 0)
        {
            List<FileManager.GameFolder> foundGames = await FileManager.CrawlGames(selectedFolders.Select(x => x.Path.AbsolutePath).ToArray());

            cont_FoundGames.Children.Clear();

            foreach (FileManager.GameFolder gameFolder in foundGames)
            {
                Label ui = new Label();
                //ui.Draw(gameFolder);
                ui.Content = gameFolder;
                ui.Height = 30;
                ui.Margin = new Thickness(0, 0, 0, 5);

                cont_FoundGames.Children.Add(ui);
                availableImports.Push(gameFolder);
            }
        }
    }

    private async Task AttemptImport()
    {
        if (possibleLibraries == null)
            return;

        // move me into the file manager please

        dbo_Libraries chosenLibary = possibleLibraries!.First();

        //if (MessageBox.Show($"Are you sure you want to import the following games into the libary \n'{chosenLibary.rootPath}'?", "Import", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        //{
        //    return;
        //}

        bool useGuidFolderNames = await ConfigHandler.GetConfigValue<bool>(ConfigHandler.ConfigValues.Import_GUIDFolderNames, true);

        while (availableImports.TryPop(out FileManager.GameFolder folder))
        {
            try
            {
                string gameFolderName = CorrectGameName(Path.GetFileName(folder.path));

                dbo_Game newGame = new dbo_Game
                {
                    gameName = gameFolderName,
                    gameFolder = useGuidFolderNames ? Guid.NewGuid().ToString() : gameFolderName,
                    executablePath = TryFindBestExecutable(folder.executables),
                    libaryId = chosenLibary.libaryId
                };

                (bool isInvalid, _) = await FileManager.TryMigrate(newGame);

                if (!isInvalid)
                {
                    await DatabaseHandler.InsertIntoTable(newGame);
                }
            }
            catch
            {
                //MessageBox.Show(e.Message, "Failed to import game", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //MessageBox.Show("Done", "Done", MessageBoxButton.OK);

        cont_FoundGames.Children.Clear();
        availableImports.Clear();

        await LibraryHandler.RedetectGames();
        await onReimportGames!();

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
}