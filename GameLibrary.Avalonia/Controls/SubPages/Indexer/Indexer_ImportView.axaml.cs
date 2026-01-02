using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.Avalonia.Controls.SubPage;
using GameLibrary.Avalonia.Helpers;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Controls.SubPage.Indexer;

public partial class Indexer_ImportView : UserControl
{
    private Popup_AddGames? master;

    private (int, string)[]? availableLibraries;
    private List<FileManager.IImportEntry> availableImports = new List<FileManager.IImportEntry>();

    public Indexer_ImportView()
    {
        InitializeComponent();
    }

    public async void Setup(Popup_AddGames master)
    {
        this.master = master;
        cont_FoundGames.Children.Clear();

        availableLibraries = await LibraryHandler.GetLibraries();
        string[] libraries = ["No Library", .. availableLibraries.Select(x => x.Item2)];

        inp_Library.Setup(libraries, 0, null);

        btn_Search.RegisterClick(ScanDirectory);
        btn_Import.RegisterClick(AttemptImport);

        btn_SelectFile.RegisterClick(AddFile);
        btn_SelectFolder.RegisterClick(AddFolder);
    }

    private async Task ScanDirectory()
    {
        IReadOnlyList<IStorageFolder> selectedFolders = await DialogHelper.OpenFolderAsync("Select Directories", true);

        if (selectedFolders.Count > 0)
        {
            availableImports.AddRange(await FileManager.CrawlGames(selectedFolders.Select(x => x.Path.AbsolutePath).ToArray()));
            UpdateAvailableGamesUI();
        }
    }

    private async Task AddFile()
    {
        IReadOnlyList<IStorageFile> selectedFiles = await DialogHelper.OpenFileAsync("Select Games", true);
        availableImports.AddRange(selectedFiles.Select(x => new FileManager.ImportEntry_Binary(x.Path.AbsolutePath)));

        UpdateAvailableGamesUI();
    }

    private async Task AddFolder()
    {
        IReadOnlyList<IStorageFolder> selectedFiles = await DialogHelper.OpenFolderAsync("Select Games", true);
        availableImports.AddRange(selectedFiles.Select(x => new FileManager.ImportEntry_Folder(null, x.Path.AbsolutePath)));

        UpdateAvailableGamesUI();
    }

    private void UpdateAvailableGamesUI()
    {
        cont_FoundGames.Children.Clear();

        foreach (FileManager.IImportEntry entry in availableImports)
        {
            if (entry is FileManager.ImportEntry_Folder gameFolder)
            {
                Indexer_Folder ui = new Indexer_Folder();
                ui.Draw(gameFolder, master!.RequestFolderView);

                cont_FoundGames.Children.Add(ui);
            }
            else if (entry is FileManager.ImportEntry_Binary gameBinary)
            {
                Indexer_File ui = new Indexer_File();
                ui.Draw(gameBinary);

                cont_FoundGames.Children.Add(ui);
            }
        }
    }

    private async Task AttemptImport()
    {
        (int, string)? selectedLibrary = inp_Library.selectedIndex == 0 ? null : availableLibraries![inp_Library.selectedIndex - 1];
        string paragraph = !selectedLibrary.HasValue ? "Import without moving" : $"Import and move files into the following directory? \n\n{selectedLibrary.Value.Item2}";

        if (await DependencyManager.uiLinker!.OpenYesNoModalAsync("Import?", paragraph, Import, "Importing"))
        {
            cont_FoundGames.Children.Clear();
            availableImports.Clear();

            await master!.onReimportGames!();
        }

        async Task Import()
        {
            await LibraryHandler.ImportGames(availableImports, selectedLibrary?.Item1);
        }
    }
}