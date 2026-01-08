using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.AvaloniaUI.Controls.SubPage;
using GameLibrary.AvaloniaUI.Helpers;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace GameLibrary.AvaloniaUI.Controls.SubPage.Indexer;

public partial class Indexer_ImportView : UserControl
{
    private Popup_AddGames? master;

    private LibraryDto[]? availableLibraries;
    private List<FileManager.IImportEntry> availableImports = new List<FileManager.IImportEntry>();

    public Indexer_ImportView()
    {
        InitializeComponent();
    }

    public async Task Setup(Popup_AddGames master)
    {
        this.master = master;
        cont_FoundGames.Children.Clear();

        var v = LibraryManager.GetLibraries();
        availableLibraries = LibraryManager.GetLibraries().Where(x => x.externalType == null).ToArray();
        string[] libraries = ["No Library", .. availableLibraries.Select(x => x.root)];

        inp_Library.Setup(libraries, 0, null);

        btn_Search.RegisterClick(ScanDirectory);
        btn_Import.RegisterClick(AttemptImport);

        btn_SelectFile.RegisterClick(AddFile);
        btn_SelectFolder.RegisterClick(AddFolder);
    }

    private async Task ScanDirectory()
    {
        string[]? selectedFolders = await DependencyManager.OpenFoldersDialog("Select Directories");

        if (selectedFolders?.Length > 0)
        {
            availableImports.AddRange(await FileManager.CrawlGames(selectedFolders));
            UpdateAvailableGamesUI();
        }
    }

    private async Task AddFile()
    {
        string[]? selectedFiles = await DependencyManager.OpenFilesDialog("Select Games");

        if (selectedFiles == null)
            return;

        availableImports.AddRange(selectedFiles.Select(x => new FileManager.ImportEntry_Binary(x)));
        UpdateAvailableGamesUI();
    }

    private async Task AddFolder()
    {
        string[]? selectedFolders = await DependencyManager.OpenFoldersDialog("Select Games");

        if (selectedFolders == null)
            return;

        availableImports.AddRange(selectedFolders.Select(x => new FileManager.ImportEntry_Folder(null, x)));
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
        LibraryDto? selectedLibrary = inp_Library.selectedIndex == 0 ? null : availableLibraries![inp_Library.selectedIndex - 1];
        string paragraph = selectedLibrary == null ? "Import without moving" : $"Import and move files into the following directory? \n\n{selectedLibrary!.root}";

        if (await DependencyManager.OpenYesNoModalAsync("Import?", paragraph, Import, "Importing"))
        {
            cont_FoundGames.Children.Clear();
            availableImports.Clear();

            await master!.onReimportGames!();
        }

        async Task Import()
        {
            // maybe move the inner logic into the dto
            await LibraryManager.ImportGames(availableImports, selectedLibrary?.libraryId);
        }
    }
}