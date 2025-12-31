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
    private Popup_AddGames master;
    private List<FileManager.FolderEntry> availableImports = new List<FileManager.FolderEntry>();

    public Indexer_ImportView()
    {
        InitializeComponent();
    }

    public void Setup(Popup_AddGames master)
    {
        this.master = master;
        cont_FoundGames.Children.Clear();

        btn_Search.RegisterClick(ScanDirectory);
        btn_Import.RegisterClick(AttemptImport);
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
            List<FileManager.FolderEntry> foundGames = await FileManager.CrawlGames(selectedFolders.Select(x => x.Path.AbsolutePath).ToArray());

            cont_FoundGames.Children.Clear();

            foreach (FileManager.FolderEntry gameFolder in foundGames)
            {
                Indexer_Folder ui = new Indexer_Folder();
                ui.Draw(gameFolder, master.RequestFolderView);

                cont_FoundGames.Children.Add(ui);
                availableImports.Add(gameFolder);
            }
        }
    }

    private async Task AttemptImport()
    {
        await LibraryHandler.ImportGames(availableImports);

        cont_FoundGames.Children.Clear();
        availableImports.Clear();

        await master.onReimportGames!();
    }
}