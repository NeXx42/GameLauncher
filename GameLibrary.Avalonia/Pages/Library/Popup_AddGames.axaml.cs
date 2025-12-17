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
                ui.Content = gameFolder.path;
                ui.Height = 30;
                ui.Margin = new Thickness(0, 0, 0, 5);

                cont_FoundGames.Children.Add(ui);
                availableImports.Push(gameFolder);
            }
        }
    }

    private async Task AttemptImport()
    {
        // move me into the file manager please

        //if (MessageBox.Show($"Are you sure you want to import the following games into the libary \n'{chosenLibary.rootPath}'?", "Import", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        //{
        //    return;
        //}

        await LibraryHandler.ImportGames(availableImports);

        //MessageBox.Show("Done", "Done", MessageBoxButton.OK);

        cont_FoundGames.Children.Clear();
        availableImports.Clear();

        await LibraryHandler.RedetectGames();
        await onReimportGames!();
    }
}